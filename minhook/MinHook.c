#include "MinHook.h"
#include <stdlib.h>

typedef struct _HOOK_ENTRY {
    LPVOID pTarget;
    LPVOID pDetour;
    LPVOID* ppOriginal;
    BYTE originalBytes[14];
    BOOL enabled;
} HOOK_ENTRY;

static HOOK_ENTRY g_hooks[256];
static int g_hookCount = 0;
static BOOL g_initialized = FALSE;

MH_STATUS MH_Initialize(void) {
    g_initialized = TRUE;
    return MH_OK;
}

MH_STATUS MH_Uninitialize(void) {
    g_initialized = FALSE;
    return MH_OK;
}

MH_STATUS MH_CreateHook(LPVOID pTarget, LPVOID pDetour, LPVOID* ppOriginal) {
    if (!g_initialized) return MH_ERROR_NOT_INITIALIZED;
    if (g_hookCount >= 256) return MH_ERROR_MEMORY_ALLOC;
    
    HOOK_ENTRY* entry = &g_hooks[g_hookCount++];
    entry->pTarget = pTarget;
    entry->pDetour = pDetour;
    entry->ppOriginal = ppOriginal;
    entry->enabled = FALSE;
    
    // 保存原始字节
    memcpy(entry->originalBytes, pTarget, 14);
    
    // 创建 trampoline
    DWORD oldProtect;
    VirtualProtect(pTarget, 14, PAGE_EXECUTE_READWRITE, &oldProtect);
    
    // 分配 trampoline
    LPVOID trampoline = VirtualAlloc(NULL, 32, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!trampoline) return MH_ERROR_MEMORY_ALLOC;
    
    // 复制原始指令
    memcpy(trampoline, entry->originalBytes, 14);
    
    // 在原始函数入口写入跳转到 detour
    BYTE jump[14] = {0xFF, 0x25, 0x00, 0x00, 0x00, 0x00};  // jmp [rip+0]
    *(ULONGLONG*)(jump + 6) = (ULONGLONG)pDetour;
    memcpy(pTarget, jump, 14);
    
    VirtualProtect(pTarget, 14, oldProtect, &oldProtect);
    
    // 设置原始函数指针
    if (ppOriginal) *ppOriginal = trampoline;
    
    entry->enabled = TRUE;
    return MH_OK;
}

MH_STATUS MH_RemoveHook(LPVOID pTarget) {
    for (int i = 0; i < g_hookCount; i++) {
        if (g_hooks[i].pTarget == pTarget) {
            DWORD oldProtect;
            VirtualProtect(pTarget, 14, PAGE_EXECUTE_READWRITE, &oldProtect);
            memcpy(pTarget, g_hooks[i].originalBytes, 14);
            VirtualProtect(pTarget, 14, oldProtect, &oldProtect);
            return MH_OK;
        }
    }
    return MH_ERROR_NOT_CREATED;
}

MH_STATUS MH_EnableHook(LPVOID pTarget) {
    return MH_OK;
}

MH_STATUS MH_DisableHook(LPVOID pTarget) {
    return MH_OK;
}
