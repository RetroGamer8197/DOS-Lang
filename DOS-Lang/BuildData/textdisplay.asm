initTextMode:
    mov ax, 0x0003
    int 0x10
    mov word [cursorX], 0
    mov word [cursorY], 0
    ret

printText:
    mov ax, [cursorY]
    mov bx, [cursorX]
    mov dl, 80
    mul dl
    add bx, ax
    shl bx, 1
    mov di, bx
    mov dx, 0xB800
    mov es, dx
    mov al, [ds:si]
    cmp al, 0x00
    jz endString
    mov [es:di], al
    call checkNewLine
    inc si
    inc word [cursorX]
    cmp word [cursorX], 0x50
    je printNewLine
    jmp printText
printNewLine:
    call newLine
    jmp printText
lineBreakInString:
    inc si
    call newLine
    jmp printText

printChar:
    mov cl, al
    mov ax, [cursorY]
    mov bx, [cursorX]
    mov dl, 80
    mul dl
    add bx, ax
    shl bx, 1
    mov di, bx
    mov dx, 0xB800
    mov es, dx
    mov al, cl
    call checkNewLine
    mov [es:di], al
    inc word [cursorX]
    cmp word [cursorX], 0x50
    je newLine
    call moveCursor
    ret

checkNewLine:
    cmp al, 0x0A
    jne return
    mov byte [es:di], 0x00
    call newLine
    dec word [cursorX]
    ret

newLine:
    mov bx, 0x00
    mov cx, [cursorY]
    mov [cursorX], bx
    inc cx
    mov [cursorY], cx
    cmp word [cursorY], 25
    jne return
    call scroll
    ret

endString:
    call moveCursor
    ret

moveCursor:
    mov ax, [cursorY]
    mov bx, [cursorX]
    mov dl, 80
    mul dl
    add bx, ax

    mov dx, 0x3D4
    mov al, 0x0F
    out dx, al

    inc dl
    mov al, bl
    out dx, al

    dec dl
    mov al, 0x0E
    out dx, al

    inc dl
    mov al, bh
    out dx, al
    ret

scroll:
    push si
    push di
    mov dx, 0xB800
    mov es, dx
    mov si, 0x00A0
    mov di, 0x0000
scrollChar:
    mov al, [es:si]
    mov [es:di], al
    inc si
    inc di
    cmp di, 4000
    jne scrollChar
    dec word [cursorY]
    pop di
    pop si
    ret

return:
    ret

cursorX dw 0
cursorY dw 0
hexstr db "0123456789ABCDEF"