@ECHO OFF

SET "NDPREL=net10.0"

SET "UNATTENDGEN_TARGET=unattend_test.xml"
SET "UNATTENDGEN_REGIONTEST=regionTest.xml"
SET "UNATTENDGEN_ARCHITECTURES=amd64,arm64"
SET "UNATTENDGEN_COMPUTERNAME=Computer001"
SET "UNATTENDGEN_PARTMODE=interactive"
SET "UNATTENDGEN_AUTOLOGON=firstadmin"
SET "UNATTENDGEN_VM=vmware"
SET "UNATTENDGEN_WIFI=no"
SET "UNATTENDGEN_TELEM=no"

CD %~dp0

CD bin\Debug\%NDPREL%

cls & unattendgen --debug --target=%UNATTENDGEN_TARGET% --regionfile=%UNATTENDGEN_REGIONTEST% --architecture=%UNATTENDGEN_ARCHITECTURES% --LabConfig --BypassNRO --ConfigSet --computername=%UNATTENDGEN_COMPUTERNAME% --tzImplicit --partmode=%UNATTENDGEN_PARTMODE% --generic --customusers --autologon=%UNATTENDGEN_AUTOLOGON% --b64obscure --vm=%UNATTENDGEN_VM% --wifi=%UNATTENDGEN_WIFI% --telem=%UNATTENDGEN_TELEM% --customscripts --hidewindows --restartexplorer --customcomponents