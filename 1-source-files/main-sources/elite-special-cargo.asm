; Elite-A Special Cargo, adapted to the C64 trading screens for Unbound.
; Reference: markmoxon/elite-a-source-code-bbc-micro, cour_buy/cour_dock.
; The deterministic offer selection and 8-bit arithmetic match Elite-A.
; Scratch tables only exist while the docked menu owns the LOAD staging page.

 SpecialCargoSlots = 16
 SpecialCargoValueWidth = 6
 SpecialCargoX = TAP%
 SpecialCargoY = SpecialCargoX + SpecialCargoSlots
 SpecialCargoLegal = SpecialCargoY + SpecialCargoSlots
 SpecialCargoHigh = SpecialCargoLegal + SpecialCargoSlots
 SpecialCargoLow = SpecialCargoHigh + SpecialCargoSlots
 SpecialCargoFeeHigh = SpecialCargoLow + SpecialCargoSlots
 ASSERT SpecialCargoSlots - 1 = 15
 ASSERT SpecialCargoFeeHigh + SpecialCargoSlots <= XX21

.SpecialCargo
 LDA #11
 JSR TRADEMODE
 LDA #9
 STA XC
 LDX #0
 JSR SpecialCargoText
 JSR NLIN4
 LDA special_cargo
 ORA special_cargo+1
 BEQ specialCargoOffers

 ; An accepted contract can be inspected again without changing its value.
 JSR SpecialCargoTarget
 JSR TT111
 LDA #4
 STA YC
 LDA #2
 STA XC
 JSR cpl
 JSR TT67
 JSR TT67
 JMP SpecialCargoValue

.specialCargoOffers
 LDA #%10000000
 STA QQ17
 LDA QQ26
 EOR QQ0
 EOR QQ1
 EOR FIST
 EOR TALLY
 STA INWK
 SEC
 LDA FIST
 ADC GCNT
 ADC cmdr_type
 STA INWK+1
 ADC INWK
 SBC special_cargox
 SBC special_cargoy
 AND #SpecialCargoSlots-1
 STA QQ25
 LDA #0
 STA INWK+3             ; Number of offers actually found
 STA INWK+6             ; System counter, stops on wrap
 JSR TT81

.specialCargoLoop
 LDA INWK+3
 CMP QQ25
 BCC specialCargoCount

.specialCargoMenu
 JSR CLYNS
 LDA INWK+3
 STA QQ25               ; The input limit is the actual menu length
 BEQ specialCargoNone
 LDA #206               ; "CARGO" followed by a question mark
 JSR prq
 JSR gnum
 BEQ specialCargoExit
 BCS specialCargoExit
 SBC #0                 ; Carry clear: convert to a zero-based index
 TAX
 STX INWK
 LDY SpecialCargoFeeHigh,X
 LDA SpecialCargoLow,X
 TAX
 JSR LCASH
 BCS specialCargoAccept
 JSR CLYNS              ; Replace the entered choice with a clean error prompt
 INC XC                 ; Align Cash? one character further right
 LDA #197               ; Failed LCASH restores cash: "CASH?"
 JSR prq
.specialCargoExit
 JMP shipBuyExit

.specialCargoNone
 LDX #SpecialCargoNoneText - SpecialCargoStrings
 JMP SpecialCargoText

.specialCargoAccept
 LDX INWK
 LDA SpecialCargoX,X
 STA special_cargox
 LDA SpecialCargoY,X
 STA special_cargoy
 CLC
 LDA SpecialCargoLegal,X
 ADC FIST
 STA FIST               ; Elite-A's byte addition, including wraparound
 LDA SpecialCargoHigh,X
 STA special_cargo+1
 LDA SpecialCargoLow,X
 STA special_cargo
 JSR BEEP               ; Confirm acceptance with the Equip Ship purchase tone
 JMP SpecialCargo

.specialCargoCount
 JSR TT20
 INC INWK+6
 BEQ specialCargoMenu
 DEC INWK
 BNE specialCargoCount
 LDX INWK+3
 LDA QQ15+3
 CMP QQ0
 BNE specialCargoStar
 LDA QQ15+1
 CMP QQ1
 BNE specialCargoStar
 JMP specialCargoNext

.specialCargoStar
 LDA QQ15+3
 EOR QQ15+5
 EOR INWK+1
 CMP FIST
 BCC specialCargoLegal
 LDA #0
.specialCargoLegal
 STA SpecialCargoLegal,X
 LDA QQ15+3
 STA SpecialCargoX,X
 SEC
 SBC QQ0
 BCS specialCargoAbsX
 EOR #$FF
 ADC #1
.specialCargoAbsX
 JSR SQUA2
 STA K+1
 LDA P
 STA K
 LDX INWK+3
 LDA QQ15+1
 STA SpecialCargoY,X
 SEC
 SBC QQ1
 BCS specialCargoAbsY
 EOR #$FF
 ADC #1
.specialCargoAbsY
 LSR A
 JSR SQUA2
 PHA
 LDA P
 CLC
 ADC K
 STA Q
 PLA
 ADC K+1
 STA R
 JSR LL5
 LDX INWK+3
 LDA QQ15+1
 EOR QQ15+5
 EOR INWK+1
 LSR A
 LSR A
 LSR A
 CMP Q
 BCS specialCargoDistance
 LDA Q
.specialCargoDistance
 ORA SpecialCargoLegal,X
 STA SpecialCargoHigh,X
 STA INWK+4
 LSR A
 ROR INWK+4
 LSR A
 ROR INWK+4
 LSR A
 ROR INWK+4
 STA INWK+5
 STA SpecialCargoFeeHigh,X
 LDA INWK+4
 STA SpecialCargoLow,X
 LDA #1
 STA XC
 CLC
 LDA INWK+3
 ADC #3
 STA YC
 LDX INWK+3
 INX
 CLC
 JSR pr2
 JSR TT162
 JSR cpl
 LDX INWK+4
 LDY INWK+5
 LDA #25
 STA XC
 SEC
 LDA #SpecialCargoValueWidth
 JSR TT11
 INC INWK+3
.specialCargoNext
 LDA INWK+1
 STA INWK
 JMP specialCargoLoop

; Only actual docking calls this, before the original story-mission checks.
.SpecialCargoDock
 LDA special_cargo
 ORA special_cargo+1
 BEQ specialCargoReturn
 LDA QQ0
 CMP special_cargox
 BNE specialCargoHalf
 LDA QQ1
 CMP special_cargoy
 BNE specialCargoHalf
 JSR SpecialCargo
 LDX special_cargo
 LDY special_cargo+1
 JSR MCASH
 LDA #0
 STA special_cargo
 STA special_cargo+1
 LDY #96
 JMP DELAY
.specialCargoHalf
 LSR special_cargo+1
 ROR special_cargo
.specialCargoReturn
 RTS

.SpecialCargoChart
 LDA special_cargo
 ORA special_cargo+1
 BEQ specialCargoReturn
 JSR TT103              ; Erase old chart crosshairs
 JSR SpecialCargoTarget
 JSR TT103              ; Draw at the delivery target (if visible)
 JMP T95                ; Snap to the system and display name/distance

.SpecialCargoTarget
 LDA special_cargox
 STA QQ9
 LDA special_cargoy
 STA QQ10
 RTS

.SpecialCargoValue
 INC XC                 ; Align VALUE with the destination, one column right
 LDX #SpecialCargoValueText - SpecialCargoStrings
 JSR SpecialCargoText
.SpecialCargoAmount
 LDX special_cargo
 LDY special_cargo+1
 SEC
 LDA #SpecialCargoValueWidth
 JSR TT11
 LDA #226               ; "CR"
 JMP TT27

.SpecialCargoText
 LDA SpecialCargoStrings,X
 BEQ specialCargoReturn
 JSR DASC
 INX
 BNE SpecialCargoText

; Print in Inventory's spare row without changing its fuel/commodity layout.
; Look up the name without TT111, which would move the chart cursor and QQ8.
.SpecialCargoInventory
 LDA special_cargo
 ORA special_cargo+1
 BEQ specialCargoInventoryReturn
 LDX #5
.specialCargoSeedSave
 LDA QQ15,X
 PHA
 DEX
 BPL specialCargoSeedSave
 JSR TT81
 LDA #0
 STA U                  ; Visit at most 256 systems, even with an invalid target
.specialCargoNameSearch
 LDA QQ15+3
 CMP special_cargox
 BNE specialCargoNameNext
 LDA QQ15+1
 CMP special_cargoy
 BEQ specialCargoInventoryPrint
.specialCargoNameNext
 JSR TT20
 INC U
 BNE specialCargoNameSearch
 BEQ specialCargoSeedRestore ; No matching system: leave the spare row blank
.specialCargoInventoryPrint
 DEC YC
 LDX #SpecialCargoInventoryText - SpecialCargoStrings
 JSR SpecialCargoText
 JSR cpl
 JSR TT162
 LDA #0
 STA QQ17               ; Upper-case CR, then restore Sentence Case for Fuel
 JSR SpecialCargoAmount
 JSR TT69
.specialCargoSeedRestore
 LDX #0
.specialCargoSeedRestoreLoop
 PLA
 STA QQ15,X
 INX
 CPX #6
 BNE specialCargoSeedRestoreLoop
.specialCargoInventoryReturn
 RTS

.SpecialCargoStrings
 EQUS "SPECIAL CARGO"
 EQUB 0
.SpecialCargoValueText
 EQUS "VALUE: "
 EQUB 0
.SpecialCargoNoneText
 EQUS "NO CONTRACTS"
 EQUB 0
.SpecialCargoInventoryText
 EQUS "Cargo:"
 EQUB 0
.SpecialCargoStringsEnd
 ASSERT SpecialCargoStringsEnd - SpecialCargoStrings < 256
 ; Label, eight-letter name, space, amount field and " CR" fit inside the border.
 ASSERT SpecialCargoStringsEnd - SpecialCargoInventoryText - 1 + 8 + 1 + SpecialCargoValueWidth + 3 <= 30
