initVGAMode:
    mov ax, 0x0013
    int 0x10
    ret

plotPixel:
    mov ax, 320
    mul dx
    add ax, bx
    mov bx, 0xA000
    mov es, bx
    mov di, ax
    mov [es:di], cl
    ret