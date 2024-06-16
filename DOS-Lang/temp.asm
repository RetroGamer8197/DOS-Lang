[ORG 0x1000]
jmp start
start:
call initTextMode
mov si, var0
call printText
mov si, var1
call printText
mov bl,[var2]
call loopCheckKey
call initVGAMode
mov bx,10
mov dx,10
mov cl,4
call plotPixel
mov bx,10
mov dx,20
mov cl,5
call plotPixel
mov bl,[var9]
call loopCheckKey

ret
%include "BuildData/textdisplay.asm"
%include "BuildData/keyboard.asm"
%include "BuildData/VGA.asm"
var0 db "hello world", 10, "",0
var1 db "this is the first program in this language",0
var2 db 0x1C
var9 db 0x1C
times 512-($-$$) db 0