; Standalone PAL C64 -> 1541 sector writer. No Elite game code is modified.
; All 683 logical sectors are written and compared, including free sectors.
; Turbo pulse reader adapted from the project's elite-tape-loader.asm.
CPU_PORT = $01
VIC_D011 = $D011
VIC_BORDER = $D020
CIA1_ICR = $DC0D
CIA2_TALO = $DD04
CIA2_TAHI = $DD05
CIA2_ICR = $DD0D
CIA2_CRA = $DD0E
TPTR = $FB
TLEN = $FD
TEXT = $F9
BUFFER = $4000
BUFFER_END = $6000
SECTOR_BUFFER = $6000
TRACK_COUNT = 35
ENTRY_ADDRESS = $0810
CHROUT = $FFD2
CHRIN = $FFCF
CLRCHN = $FFCC
CHKOUT = $FFC9
CHKIN = $FFC6
READST = $FFB7

ORG $07FF
EQUW $0801
.Basic
 EQUW BasicEnd
 EQUW 10
 EQUB $9E
 EQUS STR$(ENTRY_ADDRESS)
 EQUB 0
.BasicEnd
 EQUW 0
 SKIP ENTRY_ADDRESS - P%
.Entry
 ASSERT Entry = ENTRY_ADDRESS
 CLD
 JSR $FFE7
 LDA #LO(Welcome)
 LDY #HI(Welcome)
 JSR Print
.Confirm
 JSR $FFE4
 CMP #'Y'
 BNE Confirm
 LDA #LO(Formatting)
 LDY #HI(Formatting)
 JSR Print
 ; Command channel and one explicitly allocated direct-access buffer.
 LDA #0
 JSR $FFBD
 LDA #15
 LDX #8
 LDY #15
 JSR $FFBA
 JSR $FFC0
 BCC CommandOpen
 JMP DiskError
.CommandOpen
 LDX #15
 JSR CHKOUT
 BCC FormatReady
 JMP DiskError
.FormatReady
 LDX #0
.FormatLoop
 LDA FormatCommand,X
 STX SavedX
 JSR CHROUT
 LDX SavedX
 INX
 CPX #FormatEnd-FormatCommand
 BNE FormatLoop
 LDA #13
 JSR CHROUT
 JSR CLRCHN
 JSR CheckStatus
 LDA #1
 LDX #LO(BufferName)
 LDY #HI(BufferName)
 JSR $FFBD
 LDA #2
 LDX #8
 LDY #2
 JSR $FFBA
 JSR $FFC0
 BCC BufferOpen
 JMP DiskError
.BufferOpen
 JSR CheckStatus
 LDA #0
 STA TrackIndex
.NextTrack
 LDX TrackIndex
 LDA TrackOrder,X
 STA Track
 LDA #LO(Loading)
 LDY #HI(Loading)
 JSR Print
 LDA Track
 JSR PrintNumber
 LDA #13
 JSR CHROUT
 SEI
 LDA VIC_D011
 STA SavedDisplay
 AND #$EF
 STA VIC_D011
 JSR MotorOn
 JSR InitTurbo
 JSR LoadTrack
 PHP
 JSR StopTurbo
 LDA SavedDisplay
 STA VIC_D011
 ; Restore CIA timers/interrupt masks after the tape reader.
 JSR $FF84
 PLP
 BCC TapeLoaded
 JMP TapeError
.TapeLoaded
 CLI
 JSR CheckCrc
 BCC CrcOk
 JMP TapeError
.CrcOk
 LDA #LO(Writing)
 LDY #HI(Writing)
 JSR Print
 LDA Track
 JSR PrintNumber
 LDA #13
 JSR CHROUT
 LDA #LO(BUFFER)
 STA TPTR
 LDA #HI(BUFFER)
 STA TPTR+1
 LDA #0
 STA Sector
.NextSector
 LDY #0
 LDA (TPTR),Y
 STA SectorTag
 JSR Advance
 LDY #0
.Unpack
 LDA #0
 LDX SectorTag
 BEQ StoreByte
 LDA (TPTR),Y
.StoreByte
 STA SECTOR_BUFFER,Y
 INY
 BNE Unpack
 LDA SectorTag
 BEQ Unpacked
 INC TPTR+1
.Unpacked
 JSR WriteSector
 JSR VerifySector
 INC Sector
 LDX TrackIndex
 LDA Sector
 CMP SectorCounts,X
 BNE NextSector
 INC TrackIndex
 LDA TrackIndex
 CMP #TRACK_COUNT
 BEQ Finished
 JMP NextTrack
.Finished
 LDA #2
 JSR $FFC3
 ; Reload BAM from disk; never VALIDATE or otherwise change the image.
 LDX #15
 JSR CHKOUT
 LDA #'I'
 JSR CHROUT
 LDA #13
 JSR CHROUT
 JSR CLRCHN
 JSR CheckStatus
 LDA #15
 JSR $FFC3
 LDA #LO(Success)
 LDY #HI(Success)
 JSR Print
.SuccessHalt
 JMP SuccessHalt

.WriteSector
 ; Set buffer pointer to byte 0 before filling all 256 bytes.
 LDA #LO(PointerCommand)
 LDY #HI(PointerCommand)
 JSR Command
 LDX #2
 JSR CHKOUT
 BCC WriteReady
 JMP DiskError
.WriteReady
 LDA #0
 STA ByteIndex
.WriteLoop
 LDX ByteIndex
 LDA SECTOR_BUFFER,X
 JSR CHROUT
 JSR READST
 BEQ WriteByteOk
 JMP DiskError
.WriteByteOk
 INC ByteIndex
 BNE WriteLoop
 JSR CLRCHN
 LDA #'2'
 JSR SectorCommand
 JMP CheckStatus

.VerifySector
 LDA #'1'
 JSR SectorCommand
 JSR CheckStatus
 LDX #2
 JSR CHKIN
 BCC ReadReady
 JMP DiskError
.ReadReady
 LDA #0
 STA ByteIndex
.ReadLoop
 JSR CHRIN
 LDX ByteIndex
 CMP SECTOR_BUFFER,X
 BEQ ByteEqual
 JMP VerifyError
.ByteEqual
 JSR READST
 AND #$BF
 BEQ ReadByteOk
 JMP DiskError
.ReadByteOk
 INC ByteIndex
 BNE ReadLoop
 JMP CLRCHN

.SectorCommand
 STA Operation
 LDX #15
 JSR CHKOUT
 BCC SectorCommandReady
 JMP DiskError
.SectorCommandReady
 LDA #'U'
 JSR CHROUT
 LDA Operation
 JSR CHROUT
 LDA #LO(CommandTail)
 LDY #HI(CommandTail)
 JSR Print
 LDA Track
 JSR PrintNumber
 LDA #' '
 JSR CHROUT
 LDA Sector
 JSR PrintNumber
 LDA #13
 JSR CHROUT
 JMP CLRCHN
.Command
 PHA
 TYA
 PHA
 LDX #15
 JSR CHKOUT
 BCC CommandReady
 JMP DiskError
.CommandReady
 PLA
 TAY
 PLA
 JSR Print
 JMP CLRCHN

.CheckStatus
 LDX #15
 JSR CHKIN
 BCC StatusReady
 JMP DiskError
.StatusReady
 JSR CHRIN
 STA StatusFirst
 JSR CHRIN
 STA StatusSecond
 LDX #0
.StatusLoop
 JSR CHRIN
 CMP #13
 BEQ StatusDone
 INX
 CPX #80
 BNE StatusLoop
 JMP DiskError
.StatusDone
 JSR CLRCHN
 LDA StatusFirst
 CMP #'0'
 BNE DiskError
 LDA StatusSecond
 CMP #'0'
 BNE DiskError
 RTS
.DiskError
 LDA #LO(DiskMessage)
 LDY #HI(DiskMessage)
 JMP Fail
.VerifyError
 LDA #LO(VerifyMessage)
 LDY #HI(VerifyMessage)
 JMP Fail
.TapeError
 LDA #LO(TapeMessage)
 LDY #HI(TapeMessage)
.Fail
 PHA
 TYA
 PHA
 JSR StopTurbo
 JSR CLRCHN
 CLI
 PLA
 TAY
 PLA
 JSR Print
 LDA Track
 JSR PrintNumber
 LDA #'/'
 JSR CHROUT
 LDA Sector
 JSR PrintNumber
 LDA #13
 JSR CHROUT
 LDA StatusFirst
 JSR CHROUT
 LDA StatusSecond
 JSR CHROUT
.FailureHalt
 JMP FailureHalt

.Print
 STA TEXT
 STY TEXT+1
 LDY #0
.PrintLoop
 LDA (TEXT),Y
 BEQ PrintDone
 TYA
 PHA
 LDA (TEXT),Y
 JSR CHROUT
 PLA
 TAY
 INY
 BNE PrintLoop
.PrintDone
 RTS
.PrintNumber
 LDX #'0'
.Tens
 CMP #10
 BCC Digits
 SEC
 SBC #10
 INX
 BNE Tens
.Digits
 PHA
 TXA
 JSR CHROUT
 PLA
 CLC
 ADC #'0'
 JMP CHROUT
.Advance
 INC TPTR
 BNE Advanced
 INC TPTR+1
.Advanced
 RTS

; CRC-16/CCITT-FALSE over the complete sparse track, checked against the
; generated resident table before any sector of this track is written.
.CheckCrc
 LDA #$FF
 STA Crc
 STA Crc+1
 LDA #LO(BUFFER)
 STA TPTR
 LDA #HI(BUFFER)
 STA TPTR+1
 LDX TrackIndex
 LDA LengthLo,X
 STA TLEN
 LDA LengthHi,X
 STA TLEN+1
.CrcByte
 LDY #0
 LDA (TPTR),Y
 EOR Crc+1
 STA Crc+1
 LDX #8
.CrcBit
 ASL Crc
 ROL Crc+1
 BCC CrcNext
 LDA Crc
 EOR #$21
 STA Crc
 LDA Crc+1
 EOR #$10
 STA Crc+1
.CrcNext
 DEX
 BNE CrcBit
 JSR Advance
 LDA TLEN
 BNE CrcDec
 DEC TLEN+1
.CrcDec
 DEC TLEN
 LDA TLEN
 ORA TLEN+1
 BNE CrcByte
 LDX TrackIndex
 LDA Crc
 CMP CrcLo,X
 BNE CrcBad
 LDA Crc+1
 CMP CrcHi,X
 BNE CrcBad
 CLC
 RTS
.CrcBad
 SEC
 RTS

.MotorOn
 LDA CPU_PORT
 ORA #8
 AND #$DF
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
 LDA CPU_PORT
 ORA #$28
 STA CPU_PORT
 RTS
.GetBit
 LDA #$10
.WaitPulse
 BIT CIA1_ICR
 BEQ WaitPulse
 LDA CIA2_ICR
 PHA
 LDA #$19
 STA CIA2_CRA
 PLA
 LSR A
 RTS
.GetByte
 LDA #1
 STA ByteBuffer
.BitLoop
 JSR GetBit
 ROL ByteBuffer
 BCC BitLoop
 INC VIC_BORDER
 LDA ByteBuffer
 RTS
.Sync
 LDA #0
 STA ByteBuffer
.Find02
 JSR GetBit
 ROL ByteBuffer
 LDA ByteBuffer
 CMP #2
 BNE Find02
.FindNine
 JSR GetByte
 CMP #2
 BEQ FindNine
 CMP #9
 BNE Sync
 LDX #8
.Countdown
 JSR GetByte
 STX SyncExpected
 CMP SyncExpected
 BNE Sync
 DEX
 BNE Countdown
 JMP GetByte
.LoadTrack
 JSR Sync
 CMP Track
 BNE HeaderError
 STA HeaderXor
 JSR GetByte
 STA TPTR
 EOR HeaderXor
 STA HeaderXor
 JSR GetByte
 STA TPTR+1
 EOR HeaderXor
 STA HeaderXor
 JSR GetByte
 STA TLEN
 EOR HeaderXor
 STA HeaderXor
 JSR GetByte
 STA TLEN+1
 EOR HeaderXor
 STA HeaderXor
 JSR GetByte
 CMP HeaderXor
 BNE HeaderError
 LDA TPTR
 CMP #LO(BUFFER)
 BNE HeaderError
 LDA TPTR+1
 CMP #HI(BUFFER)
 BNE HeaderError
 LDX TrackIndex
 LDA TLEN
 CMP LengthLo,X
 BNE HeaderError
 LDA TLEN+1
 CMP LengthHi,X
 BNE HeaderError
 JMP HeaderOk
.HeaderError
 SEC
 RTS
.HeaderOk
 LDA #0
 STA DataXor
 LDY #0
.LoadLoop
 LDA TLEN
 ORA TLEN+1
 BEQ LoadDone
 JSR GetByte
 STA (TPTR),Y
 EOR DataXor
 STA DataXor
 INC TPTR
 BNE LoadPointerOk
 INC TPTR+1
.LoadPointerOk
 LDA TLEN
 BNE LoadDec
 DEC TLEN+1
.LoadDec
 DEC TLEN
 JMP LoadLoop
.LoadDone
 JSR GetByte
 CMP DataXor
 BNE HeaderError
 CLC
 RTS

.Welcome
 EQUB 147
 INCLUDE "output/title.asm"
 EQUB 13
 EQUS "TAP D64 TRANSFER - DRIVE 8"
 EQUB 13,13
 EQUS "ERASE ENTIRE DISK IN DRIVE 8?"
 EQUB 13
 EQUS "INSERT TARGET DISK. PRESS Y TO START."
 EQUB 13,0
.Formatting
 EQUS "FORMATTING DRIVE 8..."
 EQUB 13,0
.Loading
 EQUS "LOADING TRACK "
 EQUB 0
.Writing
 EQUS "WRITE / VERIFY TRACK "
 EQUB 0
.Success
 EQUB 13
 EQUS "DONE - ALL 683 SECTORS VERIFIED."
 EQUB 13
 EQUS "STOP TAPE. RESET C64 TO LOAD DISK."
 EQUB 13,0
.DiskMessage
 EQUB 13
 EQUS "DISK ERROR AT TRACK/SECTOR "
 EQUB 0
.VerifyMessage
 EQUB 13
 EQUS "VERIFY MISMATCH AT TRACK/SECTOR "
 EQUB 0
.TapeMessage
 EQUB 13
 EQUS "TAPE ERROR AT TRACK/SECTOR "
 EQUB 0
.BufferName EQUS "#"
.PointerCommand EQUS "B-P:2 0"
 EQUB 13,0
.CommandTail EQUS ":2 0 "
 EQUB 0
.TrackIndex EQUB 0
.Track EQUB 0
.Sector EQUB 0
.SectorTag EQUB 0
.ByteIndex EQUB 0
.SavedX EQUB 0
.SavedDisplay EQUB 0
.Operation EQUB 0
.StatusFirst EQUB '0'
.StatusSecond EQUB '0'
.Crc EQUW 0
.ByteBuffer EQUB 0
.SyncExpected EQUB 0
.HeaderXor EQUB 0
.DataXor EQUB 0
INCLUDE "output/layout.asm"
.ProgramEnd
ASSERT ProgramEnd <= BUFFER
ASSERT BUFFER + 21 * 257 <= BUFFER_END
ASSERT BUFFER_END <= SECTOR_BUFFER
ASSERT SECTOR_BUFFER + 256 <= $A000
ASSERT SectorCounts - TrackOrder = TRACK_COUNT
ASSERT LengthLo - SectorCounts = TRACK_COUNT
PRINT "Resident end: ", ~ProgramEnd
PRINT "Resident free before buffer: ", BUFFER - ProgramEnd
PRINT "Entry: ", ~Entry
PRINT "SuccessHalt: ", ~SuccessHalt
PRINT "FailureHalt: ", ~FailureHalt
SAVE "output/transfer.prg", $07FF, ProgramEnd
