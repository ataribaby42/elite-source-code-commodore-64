; ******************************************************************************
;
; ELITE EASYFLASH BANK-0 ROML BOOTSTRAP
;
; Native EasyFlash reset enters through the vectors in bank-0 ROMH. The reset
; stub switches to 8K cartridge mode and lets the KERNAL detect this CBM80
; header. This routine performs the normal C64 reset initialisation that a
; native cartridge otherwise bypasses, copies the resident loader to $0334 and
; starts it.
;
; RAMTAS is essential here: later tape and disk commander operations swap the
; KERNAL zero page with $CE00 and require the normal reset-time values.
;
; ******************************************************************************

SRC = $F9
DST = $FB
CNT = $FD
DEST = $0334

CPU_PORT = $01
VIC_BORDER = $D020
VIC_BG = $D021

KERNAL_SCINIT = $FF81
KERNAL_IOINIT = $FF84
KERNAL_RESTOR = $FF8A
KERNAL_RAMTAS = $FD50

ORG $8000

 EQUW ColdStart
 EQUW ColdStart
 EQUB $C3, $C2, $CD, $38, $30

.ColdStart

 SEI
 CLD

 ; Reproduce the KERNAL reset initialisation skipped by native cartridge boot.
 JSR KERNAL_IOINIT
 JSR KERNAL_RAMTAS
 JSR KERNAL_RESTOR

 ; Keep SCINIT away from the resident loader's $0334-$06FF workspace.
 LDA #$08
 STA $0288
 JSR KERNAL_SCINIT
 LDA #$04
 STA $0288

 ; Tape motor off, cassette output high, and the normal loader colours.
 LDA CPU_PORT
 ORA #%00101000
 STA CPU_PORT

 LDA #2
 STA VIC_BORDER
 STA VIC_BG

 ; Preserve the six zero-page bytes used by the 16-bit copier.
 LDA $F9
 STA saveF9
 LDA $FA
 STA saveFA
 LDA $FB
 STA saveFB
 LDA $FC
 STA saveFC
 LDA $FD
 STA saveFD
 LDA $FE
 STA saveFE

 LDA #LO(LoaderImage)
 STA SRC
 LDA #HI(LoaderImage)
 STA SRC+1

 LDA #LO(DEST)
 STA DST
 LDA #HI(DEST)
 STA DST+1

 LDA #LO(LoaderImageEnd-LoaderImage)
 STA CNT
 LDA #HI(LoaderImageEnd-LoaderImage)
 STA CNT+1

 LDY #0

.copyLoop

 LDA CNT
 ORA CNT+1
 BEQ copyDone

 LDA (SRC),Y
 STA (DST),Y

 INC SRC
 BNE srcOK
 INC SRC+1

.srcOK

 INC DST
 BNE dstOK
 INC DST+1

.dstOK

 LDA CNT
 BNE countLowNonzero
 DEC CNT+1

.countLowNonzero

 DEC CNT
 JMP copyLoop

.copyDone

 LDA saveF9
 STA $F9
 LDA saveFA
 STA $FA
 LDA saveFB
 STA $FB
 LDA saveFC
 STA $FC
 LDA saveFD
 STA $FD
 LDA saveFE
 STA $FE

 JMP DEST

.saveF9 EQUB 0
.saveFA EQUB 0
.saveFB EQUB 0
.saveFC EQUB 0
.saveFD EQUB 0
.saveFE EQUB 0

.LoaderImage
 INCBIN "3-assembled-output/elite-easyflash-loader.bin"
.LoaderImageEnd

.LowBootEnd

PRINT "Elite EasyFlash bank-0 ROML bootstrap"
PRINT "ROM image size: ", LowBootEnd - $8000
PRINT "Loader size:    ", LoaderImageEnd - LoaderImage

IF LowBootEnd > $A000
 ERROR "EasyFlash ROML bootstrap exceeds 8 KiB"
ENDIF

SAVE "3-assembled-output/elite-easyflash-boot-low.bin", $8000, LowBootEnd
