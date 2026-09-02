; ******************************************************************************
;
; ELITE EASYFLASH BANK-0 ROMH RESET STUB
;
; EasyFlash starts in ultimax mode, with the reset vector supplied by ROMH.
; Copy a tiny trampoline into zero page, switch the cartridge to 8K mode and
; enter the normal KERNAL reset path. The KERNAL then recognises the CBM80
; header in bank-0 ROML and calls the cartridge bootstrap at $8009.
;
; ******************************************************************************

CPU_DDR = $00
CPU_PORT = $01
EF_CONTROL = $DE02
EF_8K = $06

ORG $FC00

.ResetEntry

 JMP ResetStart

.NmiEntry

 RTI

.ResetStart

 SEI
 CLD
 LDX #$FF
 TXS

 LDA #$37
 STA CPU_PORT
 LDA #$2F
 STA CPU_DDR

 LDX #TrampolineEnd-Trampoline-1

.copyTrampoline

 LDA Trampoline,X
 STA $0002,X
 DEX
 BPL copyTrampoline

 JMP $0002

.Trampoline

 LDA #EF_8K
 STA EF_CONTROL
 JMP ($FFFC)

.TrampolineEnd

.HighBootEnd

PRINT "Elite EasyFlash bank-0 ROMH reset stub"
PRINT "ROM image size: ", HighBootEnd - $FC00

IF HighBootEnd > $FFFA
 ERROR "EasyFlash ROMH reset stub overlaps the cartridge vectors"
ENDIF

SAVE "3-assembled-output/elite-easyflash-boot-high.bin", $FC00, HighBootEnd
