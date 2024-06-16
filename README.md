# DOS-Lang

DOS-Lang is a language I am developing to make it easier to create applications for my OS, DOS-ASM.



Currently, the language has the following instructions available:

**Text Mode Functions:**

`print(string value)` - prints to the screen. "\n" can be used to make a new line

`enterTextMode()` - enters text mode. Also has the effect of clearing the screen

**Keyboard Functions:**

`waitForKey(byte keyScanCode)` - pauses execution until the given key is pressed

**VGA Graphics Functions:**

`enterGraphicsMode()` - changes the VGA video mode to 320x200 256 colors

`plotPixel(word x, word y, byte color)` - plots a pixel on the VGA graphics screen at (`x`, `y`) of color `color`
