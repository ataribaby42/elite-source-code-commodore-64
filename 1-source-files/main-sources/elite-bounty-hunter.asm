; ******************************************************************************
;
; Optional Fer-de-Lance bounty-hunter spawn fix, independent of Unbound.
; Called by NWSHP after the blueprint flags have been merged into NEWB.
;
; Entry: Y is FER_DE_LANCE_TYPE, A is the final NEWB byte.
; Only a bounty hunter below legal status 40 loses hostile bit 2. All other
; role bits, AI aggression, police ships and later retaliation stay intact.
; The normal hostile spawn remains hostile at 40 or more; the existing
; TACTICS test can also activate a neutral hunter if FIST rises later.
;
; New Unbound dials place this routine after the RLE stream; old Unbound
; dials use HICODE. Original-game builds use LOCODE, preserving their small
; HICODE reserve without requiring Unbound.
;
; ******************************************************************************

.BountyHunterSpawnFix
 AND #%00000010         ; The caller has already checked the hull type
 BEQ bountyHunterSpawnDone
 LDA FIST
 CMP #40                ; Decimal 40: the same threshold as TACTICS
 BCS bountyHunterSpawnDone
 LDA NEWB
 AND #%11111011         ; Clear only hostility; keep every other role flag
 STA NEWB
.bountyHunterSpawnDone
 RTS
.BountyHunterSpawnFixEnd

 ASSERT BountyHunterSpawnFixEnd - BountyHunterSpawnFix = 18
