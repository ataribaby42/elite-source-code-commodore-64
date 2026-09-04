BEEBASM?=beebasm
PYTHON?=python
C1541?=c1541

GMA86_DISK=5-compiled-game-disks/elite-commodore-64$(suffix).d64

define CREATE_GMA86_DISK
$(C1541) \
    -format "elite,1" \
            d64 \
            $(GMA86_DISK) \
    -attach $(GMA86_DISK) \
    -write 3-assembled-output/firebird.bin firebird \
    -write 3-assembled-output/byebyejulie.bin byebyejulie \
    -write 3-assembled-output/gma1.unprot.bin gma1 \
    -write 3-assembled-output/gma3.bin gma3 \
    -write 3-assembled-output/gma4.bin gma4 \
    -write 3-assembled-output/gma5.bin gma5 \
    -write 3-assembled-output/gma6.bin gma6 \
    -write 3-assembled-output/readme.txt "readme,s"
endef

# A make command with no arguments will build the GMA85 variant with
# encrypted binaries, checksums enabled, the standard commander and
# crc32 verification of the game binaries
#
# Optional arguments for the make command are:
#
#   variant=<release>   Build the specified variant:
#
#                         gma85-ntsc (default disk build)
#                         gma86-pal
#                         tape-pal
#                         tape-ntsc
#                         easyflash-pal
#                         easyflash-ntsc
#                         source-disk-build (the binaries we get from running a build)
#                         source-disk-files (the binaries already on the source disk)
#
#   commander=max       Start with a maxed-out commander
#
#   encrypt=no          Disable encryption and checksum routines
#
#   match=no            Do not attempt to match the original game binaries
#                       (i.e. omit workspace noise)
#
#   verify=no           Disable crc32 verification of the game binaries
#
#   laserbeam=<mode>    Set specified player laser rendering
#
#                         rays (default)
#                         ray (one thin laser ray originating from the nose of the craft, as it should be on the Cobra Mk. III)
#                         line (one laser line originating from the nose of the craft, as it should be on the Cobra Mk. III, and matching the AI ships’ lasers)
#
#   font=<font>         Set specified font
#
#                         c64 (default)
#                         zx (ZX Spectrum Elite font)
#
#   dials=<dials>       Set specified dials bitmap
#
#                         old (default)
#                         new (without ELITE label under radar and with some corners clean-up)
#
#   sights=<sights>       Set specified laser sights
#
#                         old (default, laser-type dependent sights)
#                         cross (always crosshair laser sights)
#
#   warpjunk=yes        Enables junk objects are deleted before warp engages, so they are not dragged along
#
#   iffunit=yes         Enables I.F.F. Unit replaces Energy Bomb
#
#   unbound=yes         Enables all ELITE: Unbound gameplay and UI changes
#
#   realmissiledamage=yes
#                       Makes missiles subtract 81 energy from AI ships instead
#                       of destroying them instantly
#
#   fpslimiter=yes      Enables the Elite 128-style frame limiter
#
#   inputfix=yes        Enables parallel keyboard and joystick input
#
#   planetdatafix=yes   Waits for a fresh key press before paging planet data
#
#   renderspeedups=yes  Enables faster circle rendering using mirrored points
#
#   randomspawns=yes    Enables Elite-A-style random ship spawn positions
#
#   whitecockpit=yes    Draws the cockpit borders, compass and scanner yellow color in white
#
#   scannercolorfix=yes Fixes the red scanner blip cell beside the compass when whitecockpit is disabled
#
# So, for example:
#
#   make variant=gma86-pal commander=max encrypt=no match=no verify=no
#
# will build an unencrypted PAL disk variant with a maxed-out commander,
# no workspace noise and no crc32 verification.
#
#   make variant=tape-pal encrypt=no match=no verify=no
#
# builds the PAL cassette:
#
#   5-compiled-game-tapes/elite-commodore-64-flicker-free-pal.tap
#
# Use variant=tape-ntsc for:
#
#   5-compiled-game-tapes/elite-commodore-64-flicker-free-ntsc.tap
#
# EasyFlash variants:
#
#   make variant=easyflash-pal encrypt=no match=no verify=no
#   make variant=easyflash-ntsc encrypt=no match=no verify=no
#
# The following variables are written into elite-build-options.asm depending on
# the above arguments, so they can be passed to BeebAsm:
#
# _VERSION
#   8 = Commodore 64
#
# _VARIANT
#   1 = GMA85 NTSC (default)
#   2 = GMA85 PAL
#   3 = source disk build (the binaries from running a build of the source disk)
#   4 = source disk files (the binaries already on the source disk)
#
# _MAX_COMMANDER
#   TRUE  = Maxed-out commander
#   FALSE = Standard commander
#
# _REMOVE_CHECKSUMS
#   TRUE  = Disable checksum routines
#   FALSE = Enable checksum routines
#
# _MATCH_ORIGINAL_BINARIES
#   TRUE  = Match binaries to released version (i.e. fill workspaces with noise)
#   FALSE = Zero-fill workspaces
#
# _LASER_BEAM
#   1 = rays (default)
#   2 = ray
#   3 = line
#
# _FONT
#   1 = C64 (default)
#   2 = ZX Spectrum Elite
#
# _DIALS
#   1 = Old (default)
#   2 = New
#
# _SIGHTS
#   1 = Old (default)
#   2 = Cross
#
# _WARPJUNK
#   FALSE = Old behaviour (default)
#   TRUE  = Junk objects are deleted before warp engages, so they are not dragged along
#
# _IFFUNIT
#   FALSE = Energy Bomb (default)
#   TRUE  = I.F.F. Unit
#
# _UNBOUND
#   FALSE = Original flicker-free branch gameplay (default)
#   TRUE  = ELITE: Unbound gameplay and UI changes
#
# _REAL_MISSILE_DAMAGE
#   FALSE = Missiles destroy AI ships instantly (default)
#   TRUE  = Missiles subtract 81 energy from AI ships
#
# _FPS_LIMITER
#   FALSE = Original unrestricted game-loop timing (default)
#   TRUE  = Limit game logic to one iteration every four video frames
#
# _INPUT_FIX
#   FALSE = Original mutually exclusive keyboard/joystick input (default)
#   TRUE  = Scan the keyboard while the joystick is enabled, as in Elite 128
#
# _PLANET_DATA_FIX
#   FALSE = Planet data pages advance immediately (default)
#   TRUE  = Wait for a fresh key press and release before clearing a full page
#
# _RENDER_SPEEDUPS
#   FALSE = Calculate every circle point separately (default)
#   TRUE  = Cache and mirror circle products to reduce rendering maths
#
# _RANDOM_SPAWNS
#   FALSE = Original correlated ship spawn positions (default)
#   TRUE  = Elite-A-style random ship spawn positions
#
# _WHITE_COCKPIT
#   FALSE = Original yellow cockpit border (default)
#   TRUE  = White cockpit borders, dashboard compass and scanner yellow channel
#
# _SCANNER_COLOR_FIX
#   FALSE = Preserve the original scanner palette beside the compass (default)
#   TRUE  = Fix the red blip palette when _WHITE_COCKPIT is FALSE
#
# The encrypt and verify arguments are passed to the elite-checksum.py and
# crc32.py scripts, rather than BeebAsm

ifeq ($(commander), max)
  max-commander=TRUE
else
  max-commander=FALSE
endif

ifeq ($(encrypt), no)
  unencrypt=-u
  remove-checksums=TRUE
else
  unencrypt=
  remove-checksums=FALSE
endif

ifeq ($(match), no)
  match-original-binaries=FALSE
else
  match-original-binaries=TRUE
endif

ifeq ($(variant), source-disk-build)
  variant-number=3
  folder=source-disk-build
  suffix=-flicker-free-source-disk-build
  tape-video=pal
  media-target=c64-disk
else ifeq ($(variant), source-disk-files)
  variant-number=4
  folder=source-disk-files
  suffix=-flicker-free-source-disk-files
  tape-video=pal
  media-target=c64-disk
else ifeq ($(variant), gma86-pal)
  variant-number=2
  folder=gma86-pal
  suffix=-flicker-free-gma86-pal
  tape-video=pal
  media-target=c64-disk
else ifeq ($(variant), tape-pal)
  # Tape PAL uses the PAL/GMA86 game build.
  variant-number=2
  folder=gma86-pal
  suffix=-flicker-free-pal
  tape-video=pal
  tape-output=5-compiled-game-tapes/elite-commodore-64-flicker-free-pal.tap
  media-target=c64-tape
else ifeq ($(variant), tape-ntsc)
  # Tape NTSC uses the NTSC/GMA85 game build.
  variant-number=1
  folder=gma85-ntsc
  suffix=-flicker-free-ntsc
  tape-video=ntsc
  tape-output=5-compiled-game-tapes/elite-commodore-64-flicker-free-ntsc.tap
  media-target=c64-tape
else ifeq ($(variant), easyflash-pal)
  # EasyFlash PAL uses the PAL/GMA86 game build.
  variant-number=2
  folder=gma86-pal
  suffix=-flicker-free-easyflash-pal
  easyflash-output=5-compiled-game-cartridges/elite-commodore-64-flicker-free-easyflash-pal.crt
  media-target=c64-easyflash
else ifeq ($(variant), easyflash-ntsc)
  # EasyFlash NTSC uses the NTSC/GMA85 game build.
  variant-number=1
  folder=gma85-ntsc
  suffix=-flicker-free-easyflash-ntsc
  easyflash-output=5-compiled-game-cartridges/elite-commodore-64-flicker-free-easyflash-ntsc.crt
  media-target=c64-easyflash
else
  variant-number=1
  folder=gma85-ntsc
  suffix=-flicker-free-gma85-ntsc
  tape-video=ntsc
  media-target=c64-disk
endif

ifeq ($(unbound), yes)
  ifneq ($(filter $(variant),source-disk-build source-disk-files),)
    $(error unbound=yes is supported by the GMA, tape and EasyFlash variants only; the source-disk variants exceed the original HICODE limit)
  endif
endif

ifeq ($(laserbeam), ray)
  laserbeam-number=2
else ifeq ($(laserbeam), line)
  laserbeam-number=3
else
  laserbeam-number=1
endif

ifeq ($(font), zx)
  font-number=2
else
  font-number=1
endif

ifeq ($(dials), new)
  dials-number=2
else
  dials-number=1
endif

ifeq ($(sights), cross)
  sights-number=2
else
  sights-number=1
endif

ifeq ($(warpjunk), yes)
  warpjunk-enabled=TRUE
else
  warpjunk-enabled=FALSE
endif

ifeq ($(iffunit), yes)
  iffunit-enabled=TRUE
else
  iffunit-enabled=FALSE
endif

ifeq ($(unbound), yes)
  unbound-enabled=TRUE
else
  unbound-enabled=FALSE
endif

ifeq ($(realmissiledamage), yes)
  realmissiledamage-enabled=TRUE
else
  realmissiledamage-enabled=FALSE
endif

ifeq ($(fpslimiter), yes)
  fpslimiter-enabled=TRUE
else
  fpslimiter-enabled=FALSE
endif

ifeq ($(inputfix), yes)
  inputfix-enabled=TRUE
else
  inputfix-enabled=FALSE
endif

ifeq ($(planetdatafix), yes)
  planetdatafix-enabled=TRUE
else
  planetdatafix-enabled=FALSE
endif

ifeq ($(renderspeedups), yes)
  renderspeedups-enabled=TRUE
else
  renderspeedups-enabled=FALSE
endif

ifeq ($(randomspawns), yes)
  randomspawns-enabled=TRUE
else
  randomspawns-enabled=FALSE
endif

ifeq ($(whitecockpit), yes)
  whitecockpit-enabled=TRUE
else
  whitecockpit-enabled=FALSE
endif

ifeq ($(scannercolorfix), yes)
  scannercolorfix-enabled=TRUE
else
  scannercolorfix-enabled=FALSE
endif

.PHONY: all c64-build c64-disk c64-tape c64-easyflash
all: c64-build $(media-target)

c64-build:
	$(PYTHON) 2-build-files/elite-packbits.py --check
	echo _VERSION=8 > 1-source-files/main-sources/elite-build-options.asm
	echo _VARIANT=$(variant-number) >> 1-source-files/main-sources/elite-build-options.asm
	echo _REMOVE_CHECKSUMS=$(remove-checksums) >> 1-source-files/main-sources/elite-build-options.asm
	echo _MATCH_ORIGINAL_BINARIES=$(match-original-binaries) >> 1-source-files/main-sources/elite-build-options.asm
	echo _MAX_COMMANDER=$(max-commander) >> 1-source-files/main-sources/elite-build-options.asm
	echo _LASER_BEAM=$(laserbeam-number) >> 1-source-files/main-sources/elite-build-options.asm
	echo _FONT=$(font-number) >> 1-source-files/main-sources/elite-build-options.asm
	echo _DIALS=$(dials-number) >> 1-source-files/main-sources/elite-build-options.asm
	echo _SIGHTS=$(sights-number) >> 1-source-files/main-sources/elite-build-options.asm
	echo _WARPJUNK=$(warpjunk-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _IFF_UNIT=$(iffunit-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _UNBOUND=$(unbound-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _REAL_MISSILE_DAMAGE=$(realmissiledamage-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _FPS_LIMITER=$(fpslimiter-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _INPUT_FIX=$(inputfix-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _PLANET_DATA_FIX=$(planetdatafix-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _RENDER_SPEEDUPS=$(renderspeedups-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _RANDOM_SPAWNS=$(randomspawns-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _WHITE_COCKPIT=$(whitecockpit-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	echo _SCANNER_COLOR_FIX=$(scannercolorfix-enabled) >> 1-source-files/main-sources/elite-build-options.asm
	$(BEEBASM) -i 1-source-files/main-sources/elite-data.asm -v > 3-assembled-output/compile.txt
	$(PYTHON) 2-build-files/elite-token-layout.py
	$(BEEBASM) -i 1-source-files/main-sources/elite-sprites.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-source.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-loader.asm -v >> 3-assembled-output/compile.txt
	$(PYTHON) 2-build-files/elite-loader-layout.py
ifeq ($(variant-number), 1)
	$(BEEBASM) -i 1-source-files/main-sources/elite-firebird.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-gma1.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-gma2.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-gma3.asm -v >> 3-assembled-output/compile.txt
else ifeq ($(variant-number), 2)
	$(BEEBASM) -i 1-source-files/main-sources/elite-firebird.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-gma1.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-gma2.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-gma3.asm -v >> 3-assembled-output/compile.txt
endif
	$(BEEBASM) -i 1-source-files/main-sources/elite-send.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-readme.asm -v >> 3-assembled-output/compile.txt
	$(PYTHON) 2-build-files/elite-checksum.py $(unencrypt) -rel$(variant-number)
ifneq ($(verify), no)
	@$(PYTHON) 2-build-files/crc32.py 4-reference-binaries/$(folder) 3-assembled-output
endif

c64-disk: c64-build
ifeq ($(variant-number), 1)
	@$(C1541) \
    -format "elite,1" \
            d64 \
            5-compiled-game-disks/elite-commodore-64$(suffix).d64 \
    -attach 5-compiled-game-disks/elite-commodore-64$(suffix).d64 \
    -write 3-assembled-output/firebird.bin firebird \
    -write 3-assembled-output/gma1.unprot.bin gma1 \
    -write 3-assembled-output/gma3.bin gma3 \
    -write 3-assembled-output/gma4.bin gma4 \
    -write 3-assembled-output/gma5.bin gma5 \
    -write 3-assembled-output/gma6.bin gma6 \
    -write 3-assembled-output/readme.txt "readme,s"
else ifeq ($(variant-number), 2)
	@$(CREATE_GMA86_DISK)
	@$(PYTHON) 2-build-files/elite-gma-sectors.py \
        --disk $(GMA86_DISK) \
        --gma1 3-assembled-output/gma1.unprot.bin
	@$(CREATE_GMA86_DISK)
	@$(PYTHON) 2-build-files/elite-gma-sectors.py \
        --disk $(GMA86_DISK) \
        --gma1 3-assembled-output/gma1.unprot.bin \
        --verify
endif

# -----------------------------------------------------------------------------
# Cassette build
# -----------------------------------------------------------------------------
#
# Selected by:
#
#   variant=tape-pal
#   variant=tape-ntsc
#
# On-tape KERNAL filename is simply "ELITE".
#
c64-tape: c64-build
	$(BEEBASM) -i 1-source-files/main-sources/elite-tape-loader.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-tape-boot.asm -v >> 3-assembled-output/compile.txt
	$(PYTHON) 2-build-files/elite-tape.py \
		--boot 3-assembled-output/elite-tape-boot.prg \
		--comlod 3-assembled-output/COMLOD.bin \
		--locode 3-assembled-output/LOCODE.bin \
		--hicode 3-assembled-output/HICODE.bin \
		--video $(tape-video) \
		--name ELITE \
		--full-boot \
		--output $(tape-output)

# -----------------------------------------------------------------------------
# EasyFlash cartridge build
# -----------------------------------------------------------------------------
#
# Selected using:
#
#   variant=easyflash-pal
#   variant=easyflash-ntsc
#
c64-easyflash: c64-build
	$(BEEBASM) -i 1-source-files/main-sources/elite-easyflash-loader.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-easyflash-boot.asm -v >> 3-assembled-output/compile.txt
	$(BEEBASM) -i 1-source-files/main-sources/elite-easyflash-reset.asm -v >> 3-assembled-output/compile.txt
	$(PYTHON) 2-build-files/elite-easyflash.py \
		--boot-low 3-assembled-output/elite-easyflash-boot-low.bin \
		--boot-high 3-assembled-output/elite-easyflash-boot-high.bin \
		--comlod 3-assembled-output/COMLOD.bin \
		--locode 3-assembled-output/LOCODE.bin \
		--hicode 3-assembled-output/HICODE.bin \
		--output $(easyflash-output)
