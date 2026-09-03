; ******************************************************************************
;
; ELITE EASYFLASH RESIDENT LOADER
;
; The cartridge stream contains three segments in the same order as the tape
; build:
;
;   COMLOD -> $4000
;   LOCODE -> $1D00
;   HICODE -> $6A00
;
; Bank 0 contains the native EasyFlash bootstrap. The stream starts in ROML
; bank 1 and continues through consecutive ROML banks. Once all three segments
; are in RAM, the cartridge is disabled and normal tape/disk commander I/O is
; handled by the unchanged game and KERNAL routines.
;
; ******************************************************************************

INCLUDE "1-source-files/main-sources/elite-build-options.asm"

CPU_PORT = $01
VIC_D011 = $D011
VIC_BORDER = $D020
VIC_BG = $D021

EF_BANK = $DE00
EF_CONTROL = $DE02
EF_8K_LED = $86
EF_OFF = $04

KERNAL_RESTOR = $FF8A
KERNAL_CLALL = $FFE7

; Entry points and RNG workspace exported from this build's actual assembly.
INCLUDE "3-assembled-output/elite-loader-layout.asm"

CODE% = $0334
ORG CODE%

.LoaderStart

 SEI
 CLD

 LDA CPU_PORT
 STA savePort

 LDA VIC_D011
 STA preD011
 AND #%11101111
 STA VIC_D011

 ; Keep ROML and I/O visible while reading the cartridge stream.
 LDA CPU_PORT
 AND #%11111000
 ORA #%00000111
 STA CPU_PORT

 LDA #1
 STA currentBank
 STA EF_BANK
 LDA #EF_8K_LED
 STA EF_CONTROL

 LDA #$00
 STA streamLoad+1
 LDA #$80
 STA streamLoad+2

 ; Header: "ECRT", format version 1, three segment descriptors.
 LDX #0

.headerSignatureLoop

 JSR GetRomByte
 CMP manifestSignature,X
 BNE invalidManifest
 INX
 CPX #4
 BNE headerSignatureLoop

 JSR GetRomByte
 CMP #1
 BNE invalidManifest

 JSR GetRomByte
 CMP #3
 BNE invalidManifest

 LDX #0

.descriptorLoop

 JSR GetRomByte
 STA segmentDestLo,X
 JSR GetRomByte
 STA segmentDestHi,X
 JSR GetRomByte
 STA segmentLenLo,X
 JSR GetRomByte
 STA segmentLenHi,X
 JSR GetRomByte
 STA segmentExpected,X

 INX
 CPX #3
 BNE descriptorLoop

 ; Reject a malformed image before writing arbitrary RAM locations.
 LDA segmentDestLo
 BNE invalidManifest
 LDA segmentDestHi
 CMP #$40
 BNE invalidManifest

 LDA segmentDestLo+1
 BNE invalidManifest
 LDA segmentDestHi+1
 CMP #$1D
 BNE invalidManifest

 LDA segmentDestLo+2
 BNE invalidManifest
 LDA segmentDestHi+2
 CMP #$6A
 BEQ manifestOK

.invalidManifest

 JMP LoadError

.manifestOK

 LDX #0
 JSR LoadSegment
 BCC comlodLoaded
 JMP LoadError

.comlodLoaded

 ; COMLOD must run with the cartridge hidden. It finishes through $CE0E.
 LDA #EF_OFF
 STA EF_CONTROL

 LDA savePort
 STA CPU_PORT

 LDA preD011
 STA VIC_D011

 LDA #2
 STA VIC_BORDER
 STA VIC_BG

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

 ; Preserve the KERNAL zero page exactly as the GMA and tape loaders do.
 CLC
 JSR CopyZeroPage

 ; Re-enable ROML for the two remaining segments. Keep the datasette motor off
 ; and cassette output high while preserving all unrelated port bits.
 LDA CPU_PORT
 AND #%11111000
 ORA #%00101111
 STA CPU_PORT

 LDA VIC_D011
 STA postD011
 AND #%11101111
 STA VIC_D011

 LDA currentBank
 STA EF_BANK
 LDA #EF_8K_LED
 STA EF_CONTROL

 LDX #1
 JSR LoadSegment
 BCC locodeLoaded
 JMP LoadError

.locodeLoaded

 LDX #2
 JSR LoadSegment
 BCC hicodeLoaded
 JMP LoadError

.hicodeLoaded

 LDA #EF_OFF
 STA EF_CONTROL

 LDA postD011
 STA VIC_D011

 LDA #0
 STA VIC_BORDER

 ; Match the original post-loader memory map: KERNAL and I/O visible, BASIC
 ; hidden. The EasyFlash hardware remains disabled until the next reset.
 LDA CPU_PORT
 AND #%11111000
 ORA #%00000110
 STA CPU_PORT

 SEC
 JSR CopyZeroPage

 JSR KERNAL_RESTOR
 JSR KERNAL_CLALL

IF _UNBOUND             ; ELITE: Unbound CRT stardust fix (begin)

.SeedGameRandom

 ; Native cartridge boot leaves RAND zeroed, unlike the normal tape/disk
 ; loading path. This makes the initial stars coincide and cancel via XOR.
 ; Use the working seed observed after tape/disk boot, before any game code
 ; consumes it. Only the live game workspace changes: leave the saved KERNAL
 ; zero page at $CE00 untouched for subsequent commander tape/disk I/O.
 ASSERT GAME_RAND > CPU_PORT
 ASSERT GAME_RAND + 3 < $100

 LDA #$00
 STA GAME_RAND
 LDA #$AA
 STA GAME_RAND+1
 LDA #$B1
 STA GAME_RAND+2
 LDA #$91
 STA GAME_RAND+3

ENDIF                   ; ELITE: Unbound CRT stardust fix (end)

 JMP GAME_ENTRY

; ******************************************************************************
; Load segment X and verify its XOR checksum.
; ******************************************************************************

.LoadSegment

 LDA segmentDestLo,X
 STA storeInstruction+1
 LDA segmentDestHi,X
 STA storeInstruction+2

 LDA segmentLenLo,X
 STA remainingLo
 LDA segmentLenHi,X
 STA remainingHi

 LDA #0
 STA segmentXor

.segmentLoop

 LDA remainingLo
 ORA remainingHi
 BEQ segmentDone

 JSR GetRomByte
 STA pendingByte
 EOR segmentXor
 STA segmentXor

 LDA pendingByte
 JSR StoreByte

 INC storeInstruction+1
 BNE storeAddressDone
 INC storeInstruction+2

.storeAddressDone

 LDA remainingLo
 BNE remainingLowNonzero
 DEC remainingHi

.remainingLowNonzero

 DEC remainingLo
 JMP segmentLoop

.segmentDone

 LDA segmentXor
 CMP segmentExpected,X
 BEQ segmentOK

 SEC
 RTS

.segmentOK

 CLC
 RTS

; ******************************************************************************
; Return the next byte from the sequential ROML stream.
; ******************************************************************************

.GetRomByte

.streamLoad

 LDA $8000
 PHA

 INC streamLoad+1
 BNE streamByteReady

 INC streamLoad+2
 LDA streamLoad+2
 CMP #$A0
 BNE streamByteReady

 INC currentBank
 LDA currentBank
 STA EF_BANK

 LDA #$80
 STA streamLoad+2

.streamByteReady

 PLA
 RTS

; ******************************************************************************
; Store A at the current segment destination. ROML is hidden only for writes to
; $8000-$9FFF, ensuring those bytes reach the underlying C64 RAM.
; ******************************************************************************

.StoreByte

 STA pendingByte

 LDA #0
 STA cartWasHidden

 LDA storeInstruction+2
 CMP #$80
 BCC storeNow
 CMP #$A0
 BCS storeNow

 LDA #EF_OFF
 STA EF_CONTROL
 INC cartWasHidden

.storeNow

 LDA pendingByte

.storeInstruction

 STA $FFFF

 LDA cartWasHidden
 BEQ storeDone

 LDA currentBank
 STA EF_BANK
 LDA #EF_8K_LED
 STA EF_CONTROL

.storeDone

 RTS

; ******************************************************************************
; C clear: copy $0002-$00FF to $CE02-$CEFF.
; C set:   copy $CE02-$CEFF to $0002-$00FF.
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
; A malformed image or checksum failure cannot return to BASIC, so show a red
; diagnostic screen and stop.
; ******************************************************************************

.LoadError

 LDA #EF_OFF
 STA EF_CONTROL

 LDA savePort
 STA CPU_PORT

 LDA preD011
 STA VIC_D011

 LDA #2
 STA VIC_BORDER
 STA VIC_BG

.errorHalt

 JMP errorHalt

; ******************************************************************************
; Resident state.
; ******************************************************************************

.manifestSignature
 EQUS "ECRT"

.segmentDestLo
 EQUB 0, 0, 0
.segmentDestHi
 EQUB 0, 0, 0
.segmentLenLo
 EQUB 0, 0, 0
.segmentLenHi
 EQUB 0, 0, 0
.segmentExpected
 EQUB 0, 0, 0

.remainingLo
 EQUB 0
.remainingHi
 EQUB 0
.segmentXor
 EQUB 0
.pendingByte
 EQUB 0
.currentBank
 EQUB 0
.cartWasHidden
 EQUB 0
.savePort
 EQUB 0
.preD011
 EQUB 0
.postD011
 EQUB 0

.LoaderEnd

PRINT "Elite EasyFlash resident loader"
PRINT "Start: ", ~LoaderStart
PRINT "End:   ", ~LoaderEnd
PRINT "Size:  ", LoaderEnd - LoaderStart

IF LoaderEnd > $0700
 ERROR "EasyFlash resident loader exceeds $06FF"
ENDIF

SAVE "3-assembled-output/elite-easyflash-loader.bin", LoaderStart, LoaderEnd
