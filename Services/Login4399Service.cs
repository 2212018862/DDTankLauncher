using System.IO;
using System.Security.Cryptography;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DDTankLauncher.Services;

/// <summary>
/// 4399登录服务 - 支持验证码
/// </summary>
public class Login4399Service
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _handler;
    private readonly CookieContainer _cookieContainer;
    
    private const string BASE_URL = "https://ptlogin.4399.com";
    private const string LOGIN_URL = BASE_URL + "/ptlogin/login.do";
    private const string VERIFY_URL = BASE_URL + "/ptlogin/verify.do";
    private const string TOGAME_URL = "http://web.4399.com/stat/togame.php?target=ddt&server_id=S{0}";
    private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    
    public Login4399Service()
    {
        _cookieContainer = new CookieContainer();
        _handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true,
            CookieContainer = _cookieContainer
        };
        
        _httpClient = new HttpClient(_handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.4399.com/");
    }
    
    /// <summary>
    /// 登录4399 - 先检查验证码，再登录
    /// </summary>
    public async Task<LoginResult> LoginAsync(string username, string password, string? captcha = null, string? captchaId = null)
    {
        try
        {
            // 第一步：获取登录页面（获取Cookie）
            await _httpClient.GetStringAsync($"{LOGIN_URL}?appId=www_home");
            
            // 第二步：检查是否需要验证码
            var captchaResult = await CheckCaptchaAsync(username);
            if (captchaResult.NeedCaptcha)
            {
                return captchaResult;
            }
            
            // 第三步：构建登录参数
            var parameters = new Dictionary<string, string>
            {
                {"loginFrom", "uframe"},
                {"postLoginHandler", "default"},
                {"layoutSelfAdapting", "true"},
                {"externalLogin", "qq"},
                {"displayMode", "popup"},
                {"layout", "vertical"},
                {"appId", "www_home"},
                {"mainDivId", "popup_login_div"},
                {"includeFcmInfo", "false"},
                {"username", username},
                {"password", password}
            };
            
            if (!string.IsNullOrEmpty(captcha) && !string.IsNullOrEmpty(captchaId))
            {
                parameters["captcha"] = captcha;
                parameters["captchaId"] = captchaId;
            }
            
            // 第四步：提交登录
            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(LOGIN_URL, content);
            string result = await response.Content.ReadAsStringAsync();
            
            // 第五步：检查登录结果
            var cookies = _cookieContainer.GetCookies(new Uri("https://www.4399.com"));
            bool isLoggedIn = false;
            string cookieString = "";
            
            foreach (Cookie cookie in cookies)
            {
                cookieString += $"{cookie.Name}={cookie.Value};";
                if (cookie.Name == "ck_accname" || cookie.Name == "Pauth")
                {
                    isLoggedIn = true;
                }
            }
            
            if (isLoggedIn)
            {
                return new LoginResult
                {
                    Success = true,
                    Cookies = cookieString,
                    Username = username
                };
            }
            
            // 解析错误信息
            string errorMsg = "登录失败";
            var errorMatch = System.Text.RegularExpressions.Regex.Match(result, @"__errorCallback\([""']([^""'\]]+)[""']\)");
            if (errorMatch.Success)
            {
                errorMsg = errorMatch.Groups[1].Value;
            }
            
            // 检查是否需要验证码（错误登录后）
            var captchaCheck = await CheckCaptchaAsync(username);
            if (captchaCheck.NeedCaptcha)
            {
                return captchaCheck;
            }
            
            return new LoginResult
            {
                Success = false,
                ErrorMessage = errorMsg
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    /// <summary>
    /// 检查是否需要验证码
    /// </summary>
    private async Task<LoginResult> CheckCaptchaAsync(string username)
    {
        try
        {
            string verifyUrl = $"{VERIFY_URL}?username={Uri.EscapeDataString(username)}&appId=www_home&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&inputWidth=iptw2";
            
            var response = await _httpClient.GetStringAsync(verifyUrl);
            
            // 如果返回 "0"，不需要验证码
            if (response.Trim() == "0")
            {
                return new LoginResult { Success = false, NeedCaptcha = false };
            }
            
            // 解析验证码HTML
            var html = response;
            
            // 查找验证码图片URL
            var imgMatch = Regex.Match(html, @"src=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (!imgMatch.Success)
            {
                return new LoginResult { Success = false, ErrorMessage = "无法获取验证码" };
            }
            
            string captchaImgUrl = imgMatch.Groups[1].Value;
            if (captchaImgUrl.StartsWith("/"))
            {
                captchaImgUrl = BASE_URL + captchaImgUrl;
            }
            else if (!captchaImgUrl.StartsWith("http"))
            {
                captchaImgUrl = BASE_URL + "/" + captchaImgUrl;
            }
            
            // 下载验证码图片
            var imgResponse = await _httpClient.GetAsync(captchaImgUrl);
            byte[] captchaImage = await imgResponse.Content.ReadAsByteArrayAsync();
            
            // 提取 captchaId
            var idMatch = Regex.Match(html, @"captchaId=[""']?([^""'&\s]+)");
            string captchaId = idMatch.Success ? idMatch.Groups[1].Value : "";
            
            return new LoginResult
            {
                Success = false,
                NeedCaptcha = true,
                CaptchaImage = captchaImage,
                CaptchaId = captchaId,
                ErrorMessage = "需要验证码"
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = $"验证码获取失败: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// 获取游戏链接
    /// </summary>
    public async Task<string> GetGameUrlAsync(string username, string serverId, string cookies)
    {
        try
        {
            string togameUrl = string.Format(TOGAME_URL, serverId);
            
            var request = new HttpRequestMessage(HttpMethod.Get, togameUrl);
            request.Headers.Add("Cookie", cookies);
            
            var response = await _httpClient.SendAsync(request);
            string html = await response.Content.ReadAsStringAsync();
            
            var iframeMatch = Regex.Match(html, @"src=""(http[^""]*assist[^""]*)""", RegexOptions.IgnoreCase);
            if (!iframeMatch.Success)
            {
                return GetDefaultUrl(username, serverId);
            }
            
            string assistUrl = iframeMatch.Groups[1].Value;
            var assistResponse = await _httpClient.GetAsync(assistUrl);
            string assistHtml = await assistResponse.Content.ReadAsStringAsync();
            
            var movieMatch = Regex.Match(assistHtml, @"value='(Loading\.swf\?[^']*)'", RegexOptions.IgnoreCase);
            if (!movieMatch.Success)
            {
                movieMatch = Regex.Match(assistHtml, @"value=""(Loading\.swf\?[^""]*)""", RegexOptions.IgnoreCase);
            }
            
            if (!movieMatch.Success)
            {
                return GetDefaultUrl(username, serverId);
            }
            
            string movieValue = movieMatch.Groups[1].Value;
            return $"http://s{serverId}.ddt.1322game.com/{movieValue}";
        }
        catch
        {
            return GetDefaultUrl(username, serverId);
        }
    }
    
    /// <summary>
    /// AES加密密码 - 模拟 CryptoJS.AES.encrypt(password, passphrase)
    /// </summary>
    private string EncryptPassword(string password)
    {
        byte[] salt = new byte[8];
        RandomNumberGenerator.Fill(salt);
        
        byte[] key = EvpBytesToKey("lzYW5qaXVqa", salt, 16);
        byte[] iv = EvpBytesToKey("lzYW5qaXVqa", salt, 16, 16);
        
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        byte[] inputBytes = Encoding.UTF8.GetBytes(password);
        using var encryptor = aes.CreateEncryptor();
        byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        
        // CryptoJS 格式: base64("Salted__" + salt + ciphertext)
        byte[] result = new byte[8 + salt.Length + encryptedBytes.Length];
        Encoding.UTF8.GetBytes("Salted__").CopyTo(result, 0);
        salt.CopyTo(result, 8);
        encryptedBytes.CopyTo(result, 16);
        
        return Convert.ToBase64String(result);
    }
    
    private static byte[] EvpBytesToKey(string passphrase, byte[] salt, int keyLen, int offset = 0)
    {
        var d = new List<byte>();
        var prev = Array.Empty<byte>();
        
        while (d.Count < offset + keyLen)
        {
            byte[] data = prev.Concat(Encoding.UTF8.GetBytes(passphrase)).Concat(salt).ToArray();
            using var md5 = MD5.Create();
            prev = md5.ComputeHash(data);
            d.AddRange(prev);
        }
        
        return d.Skip(offset).Take(keyLen).ToArray();
    }
    
    private string GetDefaultUrl(string username, string serverId)
    {
        return $"http://s{serverId}.ddt.1322game.com/Loading.swf?user={username}&server=S{serverId}";
    }
}

public class LoginResult
{
    public bool Success { get; set; }
    public bool NeedCaptcha { get; set; }
    public byte[]? CaptchaImage { get; set; }
    public string CaptchaId { get; set; } = "";
    public string Cookies { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class AccountData
{
    public string Cookies { get; set; } = string.Empty;
    public string ServerId { get; set; } = "S132";
}
