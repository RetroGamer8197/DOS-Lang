loopCheckKey:
    mov ah, 0x00
    int 0x16
    cmp ah, bl
    jne loopCheckKey
    ret