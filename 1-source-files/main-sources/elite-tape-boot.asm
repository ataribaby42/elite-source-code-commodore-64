; ******************************************************************************
;
; ELITE V13C - SYS2061 + REAL RESIDENT TURBO TEST LOADER
;
; Expected after LOAD:
;
;   FOUND ELITE
;
; LIST:
;
;   10 SYS2061
;
; RUN:
;
;   copies elite-tape-loader.bin to $0334
;   jumps to $0334
;   loads the complete COMLOD.bin turbo block
;
; Success:
;
;   ELITE V11 COMPLETE BOOT
;
; Error:
;
;   ELITE V11 LOAD ERROR
;
; V10 loads COMLOD and executes it. After COMLOD starts, BASIC is overwritten.
;
; ******************************************************************************

SRC = $F9
DST = $FB
CNT = $FD

DEST = $0334

; PRG load address
ORG $07FF
EQUW $0801

; ------------------------------------------------------------------------------
; BASIC:
;
;   10 SYS2061
;
; ------------------------------------------------------------------------------

.BasicStart

 EQUW BasicEnd
 EQUW 10
 EQUB $9E
 EQUS "2061"
 EQUB 0

.BasicEnd
 EQUW 0


; ------------------------------------------------------------------------------
; $080D / SYS2061
; ------------------------------------------------------------------------------

.Bootstrap

 SEI
 CLD

 ; Preserve the six ZP bytes used by this 16-bit copier.
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

 ; SRC = LoaderImage
 LDA #LO(LoaderImage)
 STA SRC
 LDA #HI(LoaderImage)
 STA SRC+1

 ; DST = $0334
 LDA #LO(DEST)
 STA DST
 LDA #HI(DEST)
 STA DST+1

 ; CNT = loader size
 LDA #LO(LoaderImageEnd - LoaderImage)
 STA CNT
 LDA #HI(LoaderImageEnd - LoaderImage)
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
 BNE decLow
 DEC CNT+1

.decLow

 DEC CNT

 JMP copyLoop


.copyDone

 ; Put the caller's ZP back exactly as it was before SYS.
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


; Saved bootstrap ZP values.
.saveF9 EQUB 0
.saveFA EQUB 0
.saveFB EQUB 0
.saveFC EQUB 0
.saveFD EQUB 0
.saveFE EQUB 0


; ------------------------------------------------------------------------------
; Loader image assembled for its final execution address $0334.
; ------------------------------------------------------------------------------

.LoaderImage

 INCBIN "3-assembled-output/elite-tape-loader.bin"

.LoaderImageEnd


PRINT "Elite V13C cassette turbo bootstrap - tape filename ELITE"
PRINT "Bootstrap:   ", ~Bootstrap
PRINT "Loader dest: ", ~DEST
PRINT "Loader size: ", LoaderImageEnd - LoaderImage
PRINT "PRG size:    ", P% - $07FF

SAVE "3-assembled-output/elite-tape-boot.prg", $07FF, P%
