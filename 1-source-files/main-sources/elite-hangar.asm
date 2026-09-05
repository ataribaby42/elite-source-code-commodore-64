; ******************************************************************************
;
; Elite: Unbound BBC-style docking hangar
;
; These blocks execute from unused space in the original nine-page dashboard
; buffer. The loader places the communication text immediately before the
; hangar, which remains fixed at DSTORE% + $500 after the PackBits stream.
;
; ******************************************************************************

; Assemble the helper immediately after the actual selected PackBits stream.
; The loader appends its binary at the same position; no address is duplicated.
 ORG DSTORE%
IF _DIALS = 2
 INCBIN "1-source-files/images/C.CODIALSNEW.RLE.bin"
ELSE
 INCBIN "1-source-files/images/C.CODIALS.RLE.bin"
ENDIF

.C128RasterStart

IF _DIALS = 2
.C128Slow
 LDA #0
 STA C128_SPEED
 RTS

.C128BorderSetup
 LDA #C128_RASTER_TOP
 STA VIC+$12
ENDIF

.C128Border
 LDA #1                ; Like Elite 128, writes are harmless on an original C64
 STA C128_SPEED
 JMP C128BorderReturn

IF _BOUNTY_HUNTER_FIX AND (_DIALS = 2)
 INCLUDE "1-source-files/main-sources/elite-bounty-hunter.asm"
ENDIF

.C128RasterEnd
 ASSERT C128RasterEnd <= DSTORE% + $4D6
 SAVE "3-assembled-output/C128-RASTER.bin", C128RasterStart, C128RasterEnd

 ORG DSTORE% + $4D6

.BOUNTY_HUNTER_COMM_TEXT_CODE

.BountyHunterMessageText

.BountyHunterWrongPlaceText
 EQUS "WRONG PLACE!"
 EQUB 0

.BountyHunterHereWeGoText
 EQUS "HERE WE GO!"
 EQUB 0

.BountyHunterMyBountyText
 EQUS "MY BOUNTY!"
 EQUB 0

.BOUNTY_HUNTER_COMM_TEXT_END

; Use the six-byte gap after the text for the shared border IRQ exit.
.C128BorderReturn
 INC RASTCT            ; $FF -> 0, ready for the top-screen IRQ at line 40
 JMP COMIRQ3           ; Restore interrupted registers; no extra audio tick
.C128BorderReturnEnd

 ASSERT C128BorderReturnEnd <= DSTORE% + $500

 SAVE "3-assembled-output/BOUNTY-HUNTER-COMMS.bin", BOUNTY_HUNTER_COMM_TEXT_CODE, C128BorderReturnEnd

 ORG DSTORE% + $500

.HANGAR_CODE

; ******************************************************************************
;
;       Name: HATB
;       Type: Variable
;   Category: Ship hangar
;    Summary: BBC-style groups of ships to show in the docking hangar
;
; ******************************************************************************

.HATB

                        ; Shuttle (left) and Transporter (right)
 EQUB SHU, %01010100, %00111011
 EQUB 10,  %10000010, %10110000
 EQUB 0,   0,          0

                        ; Three cargo canisters
 EQUB OIL, %01010000, %00010001
 EQUB OIL, %11010001, %00101000
 EQUB OIL, %01000000, %00000110

                        ; Transporter (right) and Cobra Mk III (left)
 EQUB 10,  %01100000, %10010000
 EQUB CYL, %00010000, %11010001
 EQUB 0,   0,          0

                        ; Viper (right and forward) and Krait (left)
 EQUB COPS, %01010001, %11111000
 EQUB KRA,  %01100000, %01110101
 EQUB 0,    0,          0

.HANGAR_SOLO

 EQUB OIL, SHU, 10, CYL, 12, COPS, KRA

; ******************************************************************************
;
;       Name: HALL
;       Type: Subroutine
;   Category: Ship hangar
;    Summary: Draw a BBC-style ship hangar after docking
;
; ******************************************************************************

.HALL

 JSR UNWISE             ; Switch the C64 line routines from EOR to OR drawing

 LDA #0                 ; Clear the space view and draw its border
 JSR TT66

 JSR DORND              ; Choose a predefined group or one random ship
 BPL HA7

 AND #3                 ; Set X to 0, 9, 18 or 27 for one of the four groups
 STA T
 ASL A
 ASL A
 ASL A
 ADC T
 TAX

 LDY #3                 ; Draw all three entries in the selected group
 STY CNT2

.HAL8

 LDY #2

.HAL9

 LDA HATB,X             ; Copy one packed position and ship type to XX15
 STA XX15,Y
 INX
 DEY
 BPL HAL9

 TXA
 PHA
 JSR HAS1
 PLA
 TAX

 DEC CNT2
 BNE HAL8

 LDY #128               ; Mark this as a hangar containing multiple ships
 BNE HA9                ; This BNE is effectively a JMP

.HA7

 LSR A                  ; Random x-coordinate and z high byte
 STA XX15+1

 JSR DORND              ; Random z low byte and x sign
 STA XX15

 JSR DORND              ; Pick one of seven classic hangar ships, or no ship
 AND #7
 BEQ HA8
 TAX
 DEX
 LDA HANGAR_SOLO,X
 STA XX15+2

 JSR HAS1

.HA8

 LDY #0                 ; Mark this as a hangar containing at most one ship

.HA9

 STY YSAV
 JSR UNWISE             ; Restore the normal EOR line-drawing mode
 JMP HANGER             ; Draw the background behind the ships and return

; ******************************************************************************
;
;       Name: HANGER
;       Type: Subroutine
;   Category: Ship hangar
;    Summary: Draw the perspective floor and back wall of the hangar
;
; ******************************************************************************

.HANGER

 LDX #2                 ; Draw 11 floor lines for divisors 2 through 12

.HAL1

 STX XSAV
 STX Q
 LDA #130
 JSR DVID4              ; P = 130 / Q, giving perspective-spaced rows

 LDA P
 CLC
 ADC #Y                 ; Draw the row below the centre of the space view
 STA Q                  ; Store the pixel row while drawing from both sides

 LDX #0                 ; Point SC(1 0) at the left end of this pixel row
 JSR HAS4
 LDA #%00100000         ; Start at x = 2, inside the left border
 STA R2
 LDA #32
 STA T
 JSR HAS2               ; Draw right until an existing ship pixel is reached

 LDA Q                  ; Point SC(1 0) at the right end of the same row
 LDX #248
 JSR HAS4
 LDA #%00000100         ; Start at x = 253, inside the right border
 STA R2
 LDA #32
 STA T
 JSR HAS3               ; Draw left until an existing ship pixel is reached

 LDA YSAV               ; Predefined groups can have a gap between ships
 BEQ HA2

 LDA Q                  ; Draw from just right of centre towards the right
 LDX #128
 JSR HAS4
 LDA #%00100000
 STA R2
 LDA #16
 STA T
 JSR HAS2

 LDA Q                  ; Draw from just right of centre towards the left
 LDX #128
 JSR HAS4
 LDA #%01000000
 STA R2
 LDA #17
 STA T
 JSR HAS3

.HA2

 LDX XSAV
 INX
 CPX #13
 BCC HAL1

 LDX #16                ; Draw 15 back-wall lines, spaced 16 pixels apart

.HAL6

 STX XSAV
 LDA #1
 STA CNT2               ; Start below the top border

.HAL7

 LDX XSAV
 LDA CNT2
 JSR HAS4               ; Point SC(1 0) at this vertical-line pixel

 LDX XSAV
 TXA
 AND #7
 TAX
 LDA TWOS,X             ; Get the mask for this x-coordinate
 LDY #0
 AND (SC),Y
 BNE HA6                ; Stop when the line reaches a ship or the floor

 LDA TWOS,X
 ORA (SC),Y
 STA (SC),Y

 INC CNT2
 LDA CNT2
 CMP #Y+11              ; Continue down to the hangar horizon
 BCC HAL7

.HA6

 LDX XSAV
 TXA
 CLC
 ADC #16
 TAX
 BNE HAL6

 RTS

; ******************************************************************************
;
;       Name: HAS2
;       Type: Subroutine
;   Category: Ship hangar
;    Summary: Draw a hangar line right until it reaches an occupied pixel
;
; ******************************************************************************

.HAS2

 LDA R2
 AND (SC),Y
 BNE HA3

 LDA R2
 ORA (SC),Y
 STA (SC),Y

 LSR R2
 BCC HAS2

 DEC T
 BEQ HA3

 LDA SC                 ; Move one eight-pixel character block to the right
 CLC
 ADC #8
 STA SC
 BCC HANGAR_HAS2_MASK
 INC SC+1

.HANGAR_HAS2_MASK

 LDA #%10000000
 STA R2
 BNE HAS2               ; This BNE is effectively a JMP

.HA3

 RTS

; ******************************************************************************
;
;       Name: HAS3
;       Type: Subroutine
;   Category: Ship hangar
;    Summary: Draw a hangar line left until it reaches an occupied pixel
;
; ******************************************************************************

.HAS3

 LDA R2
 AND (SC),Y
 BNE HA3

 LDA R2
 ORA (SC),Y
 STA (SC),Y

 ASL R2
 BCC HAS3

 DEC T
 BEQ HA3

 LDA SC                 ; Move one eight-pixel character block to the left
 SEC
 SBC #8
 STA SC
 BCS HANGAR_HAS3_MASK
 DEC SC+1

.HANGAR_HAS3_MASK

 LDA #%00000001
 STA R2
 BNE HAS3               ; This BNE is effectively a JMP

; ******************************************************************************
;
;       Name: HAS4
;       Type: Subroutine
;   Category: Ship hangar
;    Summary: Point SC(1 0) at screen pixel row A and character block X
;
; ******************************************************************************

.HAS4

 TAY
 AND #7
 STA SC

 LDA ylookupl,Y
 CLC
 ADC SC
 STA SC
 LDA ylookuph,Y
 ADC #0
 STA SC+1

 TXA
 AND #%11111000
 CLC
 ADC SC
 STA SC
 BCC HA4
 INC SC+1

.HA4

 LDY #0
 RTS

; ******************************************************************************
;
;       Name: HAS1
;       Type: Subroutine
;   Category: Ship hangar
;    Summary: Set up and draw one static ship in the hangar
;
; ******************************************************************************

.HAS1

 JSR ZINF               ; Reset the ship workspace and orientation vectors

 LDA XX15               ; Set z_lo and take the x sign from bit 0
 STA INWK+6
 LSR A
 ROR INWK+2

 LDA XX15+1             ; Set x_hi and take z_hi from bit 0
 STA INWK
 LSR A
 LDA #1
 ADC #0
 STA INWK+7

 LDA #%10000000         ; Put the ship below the centre and rotate it on deck
 STA INWK+5
 STA RAT2

 LDA #LO(LSO)           ; Reuse the station/sun line heap for the static scene
 STA INWK+33
 LDA #HI(LSO)
 STA INWK+34

 JSR DORND              ; Apply a random yaw while keeping the ship level
 STA XSAV

.HAL5

 LDX #21
 LDY #9
 JSR MVS5
 LDX #23
 LDY #11
 JSR MVS5
 LDX #25
 LDY #13
 JSR MVS5

 DEC XSAV
 BNE HAL5

 LDY XX15+2             ; Fetch the global C64 ship type
 BEQ HA1
 STY TYPE

 TYA                    ; Look up the ship blueprint directly in XX21
 ASL A
 TAY
 LDA XX21-2,Y
 STA XX0
 LDA XX21-1,Y
 STA XX0+1

 LDY #1                 ; Read the targetable area and take its square root
 LDA (XX0),Y
 STA Q
 INY
 LDA (XX0),Y
 STA R
 JSR LL5

 LDA #100               ; Larger ships sit higher above the hangar floor
 SEC
 SBC Q
 LSR A
 STA INWK+3

 JSR TIDY
 JMP LL9                ; Draw the ship and return using a tail call

.HA1

 RTS

; ******************************************************************************
;
;       Name: UNWISE
;       Type: Subroutine
;   Category: Ship hangar
;    Summary: Toggle the main C64 line routines between EOR and OR drawing
;
; ******************************************************************************

.UNWISE

 LDX #0

.HU1

 LDA HANGAR_OPLO,X      ; Point V(1 0) at an EOR (SC),Y opcode in the line code
 STA V
 LDA HANGAR_OPHI,X
 STA V+1

 LDY #0
 LDA (V),Y
 EOR #%01000000         ; Toggle opcode $51 (EOR) and $11 (ORA)
 STA (V),Y

 INX
 CPX #HANGAR_OPHI-HANGAR_OPLO ; Visit every opcode in the patch table
 BCC HU1

 RTS

.HANGAR_OPLO

 EQUB LO(HANGAR_LI81), LO(HANGAR_LI82), LO(HANGAR_LI83), LO(HANGAR_LI84)
 EQUB LO(HANGAR_LI85), LO(HANGAR_LI86), LO(HANGAR_LI87), LO(HANGAR_LI88)
 EQUB LO(HANGAR_LI21), LO(HANGAR_LI22), LO(HANGAR_LI23), LO(HANGAR_LI24)
 EQUB LO(HANGAR_LI25), LO(HANGAR_LI26), LO(HANGAR_LI27), LO(HANGAR_LI28)
 EQUB LO(HANGAR_LIL5), LO(HANGAR_LIL6)
 EQUB LO(HANGAR_HLLEFT), LO(HANGAR_HLL1)
 EQUB LO(HANGAR_HLRIGHT), LO(HANGAR_HLSINGLE)

.HANGAR_OPHI

 EQUB HI(HANGAR_LI81), HI(HANGAR_LI82), HI(HANGAR_LI83), HI(HANGAR_LI84)
 EQUB HI(HANGAR_LI85), HI(HANGAR_LI86), HI(HANGAR_LI87), HI(HANGAR_LI88)
 EQUB HI(HANGAR_LI21), HI(HANGAR_LI22), HI(HANGAR_LI23), HI(HANGAR_LI24)
 EQUB HI(HANGAR_LI25), HI(HANGAR_LI26), HI(HANGAR_LI27), HI(HANGAR_LI28)
 EQUB HI(HANGAR_LIL5), HI(HANGAR_LIL6)
 EQUB HI(HANGAR_HLLEFT), HI(HANGAR_HLL1)
 EQUB HI(HANGAR_HLRIGHT), HI(HANGAR_HLSINGLE)

.HANGAR_OPEND

 ASSERT HANGAR_OPHI-HANGAR_OPLO > 0
 ASSERT HANGAR_OPHI-HANGAR_OPLO <= 255
 ASSERT HANGAR_OPEND-HANGAR_OPHI = HANGAR_OPHI-HANGAR_OPLO

; ******************************************************************************
;
;       Name: CommMessageSend
;       Type: Subroutine
;   Category: Flight
;    Summary: Beep and show or queue the latest ship or station communication
;
; ------------------------------------------------------------------------------
;
; Only one communication is retained. A newer message replaces an older queued
; one. Dynamic registrations are resolved immediately while a non-space screen
; is open, so an AI sender can safely leave the local bubble before display.
; ------------------------------------------------------------------------------

.CommMessageSend

 LDA #0
 STA CommMessagePrepared ; The requested message still needs its stable fields

 JSR BEEP               ; Announce receipt even when the message cannot be shown

 LDA QQ11
 BEQ commMessageShowNow ; Space views can display it immediately

 JSR CommMessagePrepareRequested
 LDA #$80
 STA CommMessagePrepared
 STA CommMessagePending ; Replace any older queued communication
 RTS

.commMessageShowNow

 LDA #0
 STA CommMessagePending ; A visible communication supersedes any stale queue
 LDA #2                 ; Private token 2 uses DLY = 100
 JMP MESS

; Set up the stardust after entering a space view, then display a queued
; communication without replaying its receipt beep.

.CommFinishSpaceView

 JSR NWSTARS
 ASL CommMessagePending ; $80 becomes 0 while its old bit 7 moves into C
 BCC commFinishSpaceViewDone
 LDA #2
 JMP MESS

.commFinishSpaceViewDone

 RTS

; Dispatch preparation according to the selected message kind. Kinds 0 to 3
; come from a station; kind 4 and future ship kinds use the saved AI slot.

.CommMessagePrepareRequested

 LDA CommMessageRequestedKind
 CMP #4
 BCS CommMessagePrepareAI
 JMP StationLaunchPrepareMessage

; Snapshot the sender AI registration and the player's recipient registration.
; The generic message fields remain unchanged while MESS later erases the text.

.CommMessagePrepareAI

 TXA
 PHA                    ; Preserve X for callers that still own a ship slot/type

 LDA CommMessageRequestedKind
 STA CommMessageKind

 LDA INF                ; Preserve the current ship data pointer while GINF
 PHA                    ; points at the AI ship sending the communication
 LDA INF+1
 PHA

 LDX CommMessageSourceSlot
 JSR GINF
 LDY #36
 LDA (INF),Y            ; Fetch the sender's final NEWB flags
 TAY

 LDX CommMessageSourceSlot
 LDA REGSEED,X
 STA CommMessageSenderNumber
 JSR AIRegistrationHidden
 BCS commMessagePrepareAIHidden

 LDA CommMessageSenderNumber
 JSR AIRegistrationLetters
 STX CommMessageSenderLetter1
 STA CommMessageSenderLetter2
 LDA #0
 STA CommMessageSenderHidden
 BEQ commMessagePrepareAISenderDone ; This BEQ is effectively a JMP

.commMessagePrepareAIHidden

 LDA #'?'
 STA CommMessageSenderLetter1
 STA CommMessageSenderLetter2
 LDA #$FF
 STA CommMessageSenderHidden

.commMessagePrepareAISenderDone

 PLA
 STA INF+1
 PLA
 STA INF

 JSR CommMessagePreparePlayerRecipient

.commMessagePrepareAIDone

 PLA
 TAX
 RTS

; Snapshot the player's registration in the stable communication fields.
; A scrambled registration is always represented as ??-???.

.CommMessagePreparePlayerRecipient

 LDA regplate_3
 STA CommMessageRecipientNumber
 LDA regplate_scrambled
 BNE commMessagePreparePlayerRecipientHidden

 LDA regplate_1
 STA CommMessageRecipientLetter1
 LDA regplate_2
 STA CommMessageRecipientLetter2
 LDA #0
 STA CommMessageRecipientHidden
 RTS

.commMessagePreparePlayerRecipientHidden

 LDA #'?'
 STA CommMessageRecipientLetter1
 STA CommMessageRecipientLetter2
 LDA #$FF
 STA CommMessageRecipientHidden
 RTS

; Uniformly choose one of three consecutive message kinds. The caller adds the
; returned index in A (0 to 2) to its group's first message kind.

.CommMessageRandomThree

 JSR DORND
 AND #3
 CMP #3
 BEQ CommMessageRandomThree
 RTS

; ******************************************************************************
;
;       Name: Pirate communication helpers
;       Type: Subroutine
;   Category: Flight
;    Summary: Optionally send one taunt per hostile pirate encounter
;
; ------------------------------------------------------------------------------
;
; NWSHP returns C set only when the member was created. Its final NEWB flags
; already include neutral-pirate handling, so bits 3 and 2 together identify a
; pirate that is still hostile. The first hostile pack member supplies the
; sender, but the chance is rolled and the communication sent only after the
; whole pack has been processed. A separately spawned hostile pirate performs
; the same single roll immediately after NWSHP.
; ------------------------------------------------------------------------------

.PiratePackMemberComplete

 BCC piratePackCountDone ; NWSHP could not create this member

 LDA CommMessageSourceSlot
 BNE piratePackCountDone ; A hostile sender is already saved for this pack

 LDA NEWB
 AND #%00001100         ; Require both pirate and hostile flags
 CMP #%00001100
 BNE piratePackCountDone

 JSR CommMessageRememberLastSlot

.piratePackCountDone

 DEC XX13
 BPL piratePackReturnCounter

 LDA CommMessageSourceSlot
 BEQ piratePackReturnCounter ; No hostile pirate was successfully created

 JSR PirateCommTrySend  ; Roll and send at most once for the whole pack

.piratePackReturnCounter

 LDA XX13               ; Restore N for the caller's BPL pack-loop test
 RTS

.HostileSingleSpawnComplete

 BCC hostileSingleSpawnDone ; NWSHP could not create this ship

 LDA NEWB
 AND #%00001100         ; Match a final pirate+hostile classification
 CMP #%00001100
 BEQ hostileSingleSpawnPirate

 LDA NEWB
 AND #%00000110         ; Otherwise require bounty-hunter and hostile flags
 CMP #%00000110
 BNE hostileSingleSpawnDone

 JSR CommMessageRememberLastSlot
 JMP BountyHunterCommTrySend ; Roll once for this hostile bounty hunter

.hostileSingleSpawnDone

 RTS

.hostileSingleSpawnPirate

 JSR CommMessageRememberLastSlot
                        ; Fall through to the single communication roll

; Apply one percentage roll to the completed encounter. Values 0 and 100 are
; compiled as never and always, without consuming the game's random generator.

.PirateCommTrySend

IF PIRATE_COMM_CHANCE_PERCENT <= 0

 RTS

ELIF PIRATE_COMM_CHANCE_PERCENT = 50

 JSR DORND
 BMI hostileSingleSpawnDone ; The high half of A gives an exact 50% rejection

ELIF PIRATE_COMM_CHANCE_PERCENT < 100

 JSR DORND
 CMP #PIRATE_COMM_CHANCE_THRESHOLD
 BCS hostileSingleSpawnDone

ENDIF

 JSR CommMessageRandomThree ; Choose one of the three spawned-pirate taunts
 ADC #4                 ; Message kinds 4 to 6 are spawned-pirate taunts
 STA CommMessageRequestedKind
 JMP CommMessageSend

; NWSHP always fills the first empty FRIN slot, so a successfully created ship
; is the final occupied slot in the compact table.

.CommMessageRememberLastSlot

 LDX #NOSH-1

.commMessageFindLastSlot

 LDA FRIN,X
 BNE commMessageRememberSlot
 DEX
 BPL commMessageFindLastSlot

.commMessageRememberSlot

 STX CommMessageSourceSlot
 RTS

; Communication text is kept in the resident space behind the PackBits
; dashboard to preserve scarce HICODE. CommMessageOffsets remains in HICODE,
; with one-byte offsets relative to CommMessageText.

.CommMessageText
 EQUS "DOCK OR LEAVE"
 EQUB 0

.ScrambleRegistrationPirateText
 EQUS "SCRAM PIRATE!"
 EQUB 0

.RunRegistrationPirateText
 EQUS "RUN PIRATE!"
 EQUB 0

.DieRegistrationPirateText
 EQUS "DIE PIRATE!"
 EQUB 0

.SpawnedPirateTauntText
 EQUS "BOO YOU DEAD!"
 EQUB 0

.SpawnedPiratePrepareDieText
 EQUS "PREPARE DIE!"
 EQUB 0

.SpawnedPirateScumbagText
 EQUS "SCUMBAG!"
 EQUB 0

.TraderHaveNiceTripText
 EQUS "HAVE NICE TRIP"
 EQUB 0

.TraderHelloText
 EQUS "HELLO"
 EQUB 0

.TraderJustPassingText
 EQUS "JUST PASSING"
 EQUB 0

.PoliceMessageText

.PoliceStopNowText
 EQUS "STOP NOW!"
 EQUB 0

.PoliceWeFoundYouText
 EQUS "WE FOUND YOU!"
 EQUB 0

.PoliceSurrenderText
 EQUS "SURRENDER!"
 EQUB 0

.HANGAR_END

 ASSERT HANGAR_END <= DSTORE% + $900

 PRINT "HANGAR"
 PRINT "Assembled at ", ~HANGAR_CODE
 PRINT "Ends at ", ~HANGAR_END
 PRINT "Code size is ", ~(HANGAR_END - HANGAR_CODE)

 SAVE "3-assembled-output/HANGAR.bin", HANGAR_CODE, HANGAR_END
