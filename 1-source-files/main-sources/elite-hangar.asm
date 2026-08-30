; ******************************************************************************
;
; Elite: Unbound BBC-style docking hangar
;
; This block executes from the unused tail of the original nine-page dashboard
; buffer. The loader places it at DSTORE% + $500 after the PackBits stream.
;
; ******************************************************************************

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
 BCC P%+4
 INC SC+1

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
 BCS P%+4
 DEC SC+1

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
 CPX #22
 BCC HU1

 RTS

.HANGAR_OPLO

 EQUB LO(LI81+2), LO(LI82+2), LO(LI83+2), LO(LI84+2)
 EQUB LO(LI85+2), LO(LI86+2), LO(LI87+2), LO(LI88+2)
 EQUB LO(LI21+2), LO(LI22+2), LO(LI23+2), LO(LI24+2)
 EQUB LO(LI25+2), LO(LI26+2), LO(LI27+2), LO(LI28+2)
 EQUB LO(LIL5+2), LO(LIL6+2)
 EQUB LO(HANGAR_HLLEFT), LO(HLL1+2)
 EQUB LO(HANGAR_HLRIGHT), LO(HANGAR_HLSINGLE)

.HANGAR_OPHI

 EQUB HI(LI81+2), HI(LI82+2), HI(LI83+2), HI(LI84+2)
 EQUB HI(LI85+2), HI(LI86+2), HI(LI87+2), HI(LI88+2)
 EQUB HI(LI21+2), HI(LI22+2), HI(LI23+2), HI(LI24+2)
 EQUB HI(LI25+2), HI(LI26+2), HI(LI27+2), HI(LI28+2)
 EQUB HI(LIL5+2), HI(LIL6+2)
 EQUB HI(HANGAR_HLLEFT), HI(HLL1+2)
 EQUB HI(HANGAR_HLRIGHT), HI(HANGAR_HLSINGLE)

.HANGAR_END

 ASSERT HANGAR_END <= DSTORE% + $900

 PRINT "HANGAR"
 PRINT "Assembled at ", ~HANGAR_CODE
 PRINT "Ends at ", ~HANGAR_END
 PRINT "Code size is ", ~(HANGAR_END - HANGAR_CODE)

 SAVE "3-assembled-output/HANGAR.bin", HANGAR_CODE, HANGAR_END
