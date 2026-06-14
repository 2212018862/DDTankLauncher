"""弹弹堂角度读取器 - 基于像素模板匹配"""
import ctypes
from ctypes import wintypes, byref
from PIL import Image
import win32gui, win32ui, win32con, time

user32 = ctypes.windll.user32

# 数字模板 (每行6像素宽, 14行高)
# 根据实际截图校准
DIGIT_TEMPLATES = {
    0: [
        ".#..#.",
        ".#..#.",
        "#....#",
        "#....#",
        "#....#",
        "#.##.#",
        "#....#",
        "#....#",
        "#....#",
        "#....#",
        ".#..#.",
        ".#..#.",
        "......",
        "......",
    ],
    1: [
        "..#...",
        ".##...",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        ".###.",
        "......",
        "......",
        "......",
    ],
    2: [
        ".#..#.",
        "#....#",
        ".....#",
        "....#.",
        "...#..",
        "..#...",
        ".#....",
        "#.....",
        "#.....",
        "#.....",
        ".####.",
        "......",
        "......",
        "......",
    ],
    3: [
        ".####.",
        "#....#",
        ".....#",
        ".....#",
        "..###.",
        ".....#",
        ".....#",
        ".....#",
        ".....#",
        "#....#",
        ".#..#.",
        "......",
        "......",
        "......",
    ],
    4: [
        "...#..",
        "..##..",
        ".#.#..",
        "#..#..",
        "#..#..",
        ".###.",
        "....#.",
        "....#.",
        "....#.",
        "....#.",
        "....#.",
        "......",
        "......",
        "......",
    ],
    5: [
        ".####.",
        "#.....",
        "#.....",
        "#.....",
        ".####.",
        ".....#",
        ".....#",
        ".....#",
        ".....#",
        "#....#",
        ".#..#.",
        "......",
        "......",
        "......",
    ],
    6: [
        ".#..#.",
        "#.....",
        "#.....",
        "#.....",
        "#.##..",
        "#....#",
        "#....#",
        "#....#",
        "#....#",
        "#....#",
        ".#..#.",
        "......",
        "......",
        "......",
    ],
    7: [
        ".####.",
        ".....#",
        "....#.",
        "...#..",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        "..#...",
        "......",
        "......",
        "......",
    ],
    8: [
        ".#..#.",
        "#....#",
        "#....#",
        "#....#",
        ".#..#.",
        "#....#",
        "#....#",
        "#....#",
        "#....#",
        "#....#",
        ".#..#.",
        "......",
        "......",
        "......",
    ],
    9: [
        ".#..#.",
        "#....#",
        "#....#",
        "#....#",
        "#....#",
        ".####.",
        ".....#",
        ".....#",
        ".....#",
        ".....#",
        ".#..#.",
        "......",
        "......",
        "......",
    ],
}

def find_hwnd():
    result = []
    def cb(hWnd, lParam):
        title = ctypes.create_string_buffer(256)
        user32.GetWindowTextA(hWnd, title, 256)
        t = title.value.decode('gbk', errors='ignore')
        if '4399' in t: result.append(hWnd)
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)(cb), 0)
    return result[0] if result else None

def capture(hwnd):
    rect = wintypes.RECT()
    user32.GetClientRect(hwnd, byref(rect))
    w, h = rect.right, rect.bottom
    hdc = win32gui.GetDC(hwnd)
    mfcDC = win32ui.CreateDCFromHandle(hdc)
    saveDC = mfcDC.CreateCompatibleDC()
    bmp = win32ui.CreateBitmap()
    bmp.CreateCompatibleBitmap(mfcDC, w, h)
    saveDC.SelectObject(bmp)
    saveDC.BitBlt((0, 0), (w, h), mfcDC, (0, 0), win32con.SRCCOPY)
    info = bmp.GetInfo()
    data = bmp.GetBitmapBits(True)
    img = Image.frombuffer('RGB', (info['bmWidth'], info['bmHeight']), data, 'raw', 'BGRX', 0, 1)
    win32gui.DeleteObject(bmp.GetHandle())
    saveDC.DeleteDC()
    mfcDC.DeleteDC()
    win32gui.ReleaseDC(hwnd, hdc)
    return img

def is_white(r, g, b, threshold=150):
    return r > threshold and g > threshold and b > threshold

def match_digit(img, x_start, y_start, template):
    """匹配单个数字"""
    score = 0
    total = 0
    
    for y, row in enumerate(template):
        for x, expected in enumerate(row):
            if expected == '.':
                continue
            
            total += 1
            px, py = x_start + x, y_start + y
            
            if 0 <= px < img.width and 0 <= py < img.height:
                r, g, b = img.getpixel((px, py))
                if is_white(r, g, b):
                    score += 1
    
    return score / max(total, 1)

def read_angle(hwnd):
    """读取角度值"""
    img = capture(hwnd)
    
    # 角度数字位置 (根据校准)
    # 十位: x=33~38, y=5~18
    # 个位: x=39~43, y=5~18
    
    best_digit1 = -1
    best_score1 = 0
    best_digit2 = -1
    best_score2 = 0
    
    # 匹配十位
    for digit, template in DIGIT_TEMPLATES.items():
        score = match_digit(img, 33, 5, template)
        if score > best_score1:
            best_score1 = score
            best_digit1 = digit
    
    # 匹配个位
    for digit, template in DIGIT_TEMPLATES.items():
        score = match_digit(img, 39, 5, template)
        if score > best_score2:
            best_score2 = score
            best_digit2 = digit
    
    if best_score1 > 0.5 and best_score2 > 0.5:
        return best_digit1 * 10 + best_digit2
    elif best_score1 > 0.5:
        return best_digit1
    else:
        return -1

hwnd = find_hwnd()
if not hwnd: print("找不到窗口"); exit()

print("=== 弹弹堂角度读取器 ===")
print("实时读取角度值\n")

for i in range(60):
    angle = read_angle(hwnd)
    ts = time.strftime("%H:%M:%S")
    if angle >= 0:
        print(f"  [{ts}] 角度: {angle}°")
    else:
        print(f"  [{ts}] 无法识别")
    time.sleep(0.5)

