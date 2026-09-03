; ******************************************************************************
;
; ELITE V13C - RELEASE TAPE NAME ELITE
;
; Tape blocks:
;
;   4  COMLOD -> $4000
;   5  LOCODE -> $1D00
;   6  HICODE -> $6A00
;
; Sequence follows the original GMA1 load3/load4 flow:
;
;   load COMLOD
;   patch $CE0E
;   JMP COMLOD_ENTRY
;   COMLOD returns via $CE0E
;   save $0002-$00FF to $CE02-$CEFF
;   load LOCODE
;   load HICODE
;   restore zero page
;   RESTOR / CLALL
;   JMP GAME_ENTRY
;
; V11 keeps the proven V8B/V10A tape rules:
;
;   * standard KERNAL block ends explicitly
;   * 2 second gap before first turbo block
;   * VIC display blanked during long turbo transfers
;
; V12 is the cleaned release version of the working V11B loader.
;
; The proven tape timing is intentionally unchanged:
;
;   COMLOD pilot: 2048 bytes
;   LOCODE pilot: 2048 bytes
;   HICODE pilot: 256 bytes
;
; No development checkpoint writes to $CF00 are performed.
;
; V13 adds the classic turbo-loader border effect:
;
;   INC $D020
;
; once per completely decoded turbo byte. This adds only 6 cycles between
; bytes and leaves the proven pulse discriminator/timer code untouched.
;
; ******************************************************************************

CPU_PORT = $01
VIC_D011 = $D011
VIC_BORDER = $D020
VIC_BG = $D021

CIA1_ICR  = $DC0D
CIA2_TALO = $DD04
CIA2_TAHI = $DD05
CIA2_ICR  = $DD0D
CIA2_CRA  = $DD0E

KERNAL_SCINIT = $FF81
KERNAL_IOINIT = $FF84
KERNAL_RESTOR = $FF8A
KERNAL_CLALL  = $FFE7

TPTR = $FB
TLEN = $FD

; Generated from the current COMLOD and LOCODE assembly, for this build.
INCLUDE "3-assembled-output/elite-loader-layout.asm"

CODE% = $0334
ORG CODE%

.LoaderStart

 SEI
 CLD

 ; Preserve only what is required if the first turbo block fails and we return
 ; to BASIC. After COMLOD has executed, BASIC is intentionally gone.
 LDA $FB
 STA saveFB
 LDA $FC
 STA saveFC
 LDA $FD
 STA saveFD
 LDA $FE
 STA saveFE

 LDA CPU_PORT
 STA savePort

 LDA VIC_D011
 STA preD011

 ; ---------------------------------------------------------------------------
 ; Block 4: COMLOD
 ; ---------------------------------------------------------------------------


 ; Blank display: remove VIC badline timing stalls.
 LDA preD011
 AND #%11101111
 STA VIC_D011

 JSR MotorOn
 JSR InitTurbo

 LDA #4
 STA expectedId
 LDA #$00
 STA expectedLo
 LDA #$40
 STA expectedHi

 JSR LoadExpected
 BCC comlodLoaded

 JMP FirstLoadError


.comlodLoaded


 JSR StopTurbo

 ; Restore the pre-load display before preparing COMLOD exactly as a normal
 ; loader would.
 LDA preD011
 STA VIC_D011

 JSR KERNAL_IOINIT
 JSR KERNAL_RESTOR

 ; Critical SCINIT screen-base fix:
 ; Resident loader occupies $0334-$06FF. With BASIC default $0288=$04,
 ; KERNAL SCINIT would initialise screen RAM at $0400 and overwrite
 ; part of our own resident loader.
 ;
 ; Use the original GMA1 ordering: point SCINIT at $0800 first,
 ; then restore $0288=$04 afterwards.
 LDA #$08
 STA $0288

 JSR KERNAL_SCINIT

 LDA #$04
 STA $0288

 ; Tape motor OFF, cassette output high.
 LDA CPU_PORT
 ORA #%00101000
 STA CPU_PORT

 ; Match the original GMA1 pre-COMLOD display setup.
 LDA #2
 STA VIC_BORDER
 STA VIC_BG

 ; COMLOD finishes with JMP $CE0E.
 LDA #$4C
 STA $CE0E
 LDA #LO(AfterCOMLOD)
 STA $CE0F
 LDA #HI(AfterCOMLOD)
 STA $CE10


 JMP COMLOD_ENTRY


; ******************************************************************************
; COMLOD has completed and jumped through $CE0E.
; ******************************************************************************

.AfterCOMLOD

 ; Do NOT add SEI here. The original GMA1 load4 begins immediately with
 ; CLC / JSR CopyZeroPage, so V12 follows that sequence exactly.


 ; Original GMA1 step 1: save the post-COMLOD zero page.
 CLC
 JSR CopyZeroPage


 ; Original GMA1 step 2: configure $01:
 ; LORAM=0, HIRAM=1, CHAREN=1, Datasette output high, motor off.
 LDA CPU_PORT
 AND #%11111000
 ORA #%00101110
 STA CPU_PORT


 ; Tape-specific addition: preserve current VIC mode and blank the display so
 ; the long LOCODE/HICODE transfers cannot suffer badline timing jitter.
 LDA VIC_D011
 STA postD011
 AND #%11101111
 STA VIC_D011


 ; ---------------------------------------------------------------------------
 ; Block 5: LOCODE -> $1D00
 ; ---------------------------------------------------------------------------

 JSR MotorOn


 JSR InitTurbo


 LDA #5
 STA expectedId
 LDA #$00
 STA expectedLo
 LDA #$1D
 STA expectedHi


 JSR LoadExpected
 BCC locodeLoaded

 JMP LateLoadError


.locodeLoaded


 ; ---------------------------------------------------------------------------
 ; Block 6: HICODE -> $6A00
 ;
 ; Motor remains on. Reinitialise the timer; the long pilot absorbs the first
 ; unclassified pulse interval.
 ; ---------------------------------------------------------------------------


 JSR InitTurbo

 LDA #6
 STA expectedId
 LDA #$00
 STA expectedLo
 LDA #$6A
 STA expectedHi

 JSR LoadExpected
 BCC hicodeLoaded

 JMP LateLoadError


.hicodeLoaded


 JSR StopTurbo

 LDA postD011
 STA VIC_D011

 ; V13A: the stripe effect leaves $D020 at an arbitrary palette value.
 ; Restore the normal Elite black border before handing control to the game.
 LDA #0
 STA VIC_BORDER

 ; Match original GMA1 memory-map setting after GMA5/GMA6 load.
 LDA CPU_PORT
 AND #%11111000
 ORA #%00000110
 STA CPU_PORT

 ; Restore the zero page saved immediately after COMLOD.
 SEC
 JSR CopyZeroPage

 JSR KERNAL_RESTOR
 JSR KERNAL_CLALL


 ; S% starts by copying zero page to $CE00 again, decrypting/unscrambling the
 ; game and calling COLD to initialise the machine.
 JMP GAME_ENTRY


; ******************************************************************************
; Standard turbo routines
; ******************************************************************************

.MotorOn

 LDA CPU_PORT
 ORA #%00001000
 AND #%11011111
 STA CPU_PORT
 RTS


.InitTurbo

 LDA #$7F
 STA CIA1_ICR
 LDA CIA1_ICR

 LDA #0
 STA CIA2_CRA

 LDA #$7F
 STA CIA2_ICR
 LDA CIA2_ICR

 LDA #$FE
 STA CIA2_TALO

 LDA #0
 STA CIA2_TAHI
 RTS


.StopTurbo

 LDA #0
 STA CIA2_CRA

 LDA #$7F
 STA CIA2_ICR
 LDA CIA2_ICR

 ; Motor off, cassette output high.
 LDA CPU_PORT
 ORA #%00101000
 STA CPU_PORT
 RTS


.GetBit

 LDA #$10

.waitPulse

 BIT CIA1_ICR
 BEQ waitPulse

 LDA CIA2_ICR
 PHA

 LDA #$19
 STA CIA2_CRA

 PLA
 LSR A
 RTS


.GetByte

 LDA #1
 STA byteBuffer

.byteLoop

 JSR GetBit
 ROL byteBuffer
 BCC byteLoop

 ; Classic C64 turbo-loader effect.
 ;
 ; Cycle the VIC-II border colour once per complete byte. INC absolute is only
 ; 6 cycles and does not alter A or carry. Loading byteBuffer afterwards also
 ; restores the same N/Z flags that the original V12 routine returned with.
 INC VIC_BORDER

 LDA byteBuffer
 RTS


.Sync

 LDA #0
 STA byteBuffer

.find02

 JSR GetBit
 ROL byteBuffer
 LDA byteBuffer
 CMP #$02
 BNE find02

.findNine

 JSR GetByte
 CMP #$02
 BEQ findNine

 CMP #$09
 BNE Sync

 LDX #8

.countdown

 JSR GetByte
 STX syncExpected
 CMP syncExpected
 BNE Sync

 DEX
 BNE countdown

 JSR GetByte
 RTS


; ******************************************************************************
; Generic turbo block loader.
;
; expectedId / expectedLo / expectedHi are set by caller.
; Length is dynamic and comes from the tape header.
;
; C clear = success
; C set   = block/header/address/length/data-XOR error
; ******************************************************************************

.LoadExpected

 JSR Sync

 CMP expectedId
 BNE blockError

 STA headerXor

 ; Destination low.
 JSR GetByte
 STA TPTR
 EOR headerXor
 STA headerXor

 ; Destination high.
 JSR GetByte
 STA TPTR+1
 EOR headerXor
 STA headerXor

 ; Length low/high.
 JSR GetByte
 STA TLEN
 EOR headerXor
 STA headerXor

 JSR GetByte
 STA TLEN+1
 EOR headerXor
 STA headerXor

 ; Header XOR.
 JSR GetByte
 CMP headerXor
 BNE blockError

 ; Validate destination.
 LDA TPTR
 CMP expectedLo
 BNE blockError

 LDA TPTR+1
 CMP expectedHi
 BNE blockError

 ; Zero length is invalid.
 LDA TLEN
 ORA TLEN+1
 BEQ blockError

 LDA #0
 STA dataXor

 LDY #0

.dataLoop

 LDA TLEN
 ORA TLEN+1
 BEQ dataDone

 JSR GetByte

 STA (TPTR),Y
 EOR dataXor
 STA dataXor

 INC TPTR
 BNE ptrOK
 INC TPTR+1

.ptrOK

 LDA TLEN
 BNE lenLow
 DEC TLEN+1

.lenLow

 DEC TLEN
 JMP dataLoop


.dataDone

 JSR GetByte
 CMP dataXor
 BNE blockError

 CLC
 RTS


.blockError

 SEC
 RTS


; ******************************************************************************
; Zero-page copy, identical in behaviour to GMA1 CopyZeroPage.
;
; C clear: $0002-$00FF -> $CE02-$CEFF
; C set:   $CE02-$CEFF -> $0002-$00FF
; ******************************************************************************

.CopyZeroPage

 LDX #2

.zpLoop

 LDA $0000,X
 BCC zpStore
 LDA $CE00,X

.zpStore

 STA $0000,X
 STA $CE00,X

 INX
 BNE zpLoop
 RTS


; ******************************************************************************
; Error paths
; ******************************************************************************

.FirstLoadError

 JSR StopTurbo

 LDA preD011
 STA VIC_D011

 JSR KERNAL_IOINIT
 JSR KERNAL_RESTOR
 JSR KERNAL_SCINIT

 LDA savePort
 STA CPU_PORT

 LDA saveFB
 STA $FB
 LDA saveFC
 STA $FC
 LDA saveFD
 STA $FD
 LDA saveFE
 STA $FE

 LDA #2
 STA VIC_BORDER

 CLI
 RTS


.LateLoadError

 ; BASIC has been overwritten by COMLOD, so do not return.
 JSR StopTurbo

 LDA postD011
 STA VIC_D011

 LDA #2
 STA VIC_BORDER
 STA VIC_BG

.errorHalt

 JMP errorHalt


; ******************************************************************************
; Resident state
; ******************************************************************************

.byteBuffer
 EQUB 0
.syncExpected
 EQUB 0
.headerXor
 EQUB 0
.dataXor
 EQUB 0

.expectedId
 EQUB 0
.expectedLo
 EQUB 0
.expectedHi
 EQUB 0

.saveFB
 EQUB 0
.saveFC
 EQUB 0
.saveFD
 EQUB 0
.saveFE
 EQUB 0
.savePort
 EQUB 0
.preD011
 EQUB 0
.postD011
 EQUB 0

.LoaderEnd

PRINT "Elite V13C turbo loader - tape filename ELITE"
PRINT "Start: ", ~LoaderStart
PRINT "End:   ", ~LoaderEnd
PRINT "Size:  ", LoaderEnd - LoaderStart
PRINT "Maximum safe resident end before COMLOD overwrite: $0700"

IF LoaderEnd > $0700
 ERROR "V13C resident loader exceeds $06FF and would be overwritten by COMLOD"
ENDIF

SAVE "3-assembled-output/elite-tape-loader.bin", LoaderStart, LoaderEnd
