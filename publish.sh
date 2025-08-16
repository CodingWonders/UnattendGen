#!/bin/bash

whereis dotnet >/dev/null 2>&1
if [[ ! $? ]]; then
	echo ".NET SDK not installed. Exiting..."
	exit 1
fi

framework="net9.0"

if [[ -d "./bin/release/$framework" ]]; then
	rm -rf "./bin/release/$framework"
fi

function selfcontainedzip {
	if [[ $# -lt 1 ]]; then
		return
	fi
	for folder in $(find "./bin/release/$1/sc" -mindepth 1 -type d); do
		if [[ ! -d "$folder" ]]; then
			continue
		fi
		name=$(basename "$folder")
		echo "Zipping $name..."
		target_zip_name=""
		case "$name" in
			"win-x86") target_zip_name="UnattendGen-x86--Windows-SelfContained.zip" ;;
			"win-x64") target_zip_name="UnattendGen-x64--Windows-SelfContained.zip" ;;
			"win-arm64") target_zip_name="UnattendGen-arm64--Windows-SelfContained.zip" ;;
			"osx-x64") target_zip_name="UnattendGen-x64--MacOS-SelfContained.zip" ;;
			"osx-arm64") target_zip_name="UnattendGen-arm64--MacOS-SelfContained.zip" ;;
			"linux-x64") target_zip_name="UnattendGen-x64--Linux-SelfContained.zip" ;;
			"linux-arm64") target_zip_name="UnattendGen-arm64--Linux-SelfContained.zip" ;;
		esac
		cd "$folder" || exit
		zip -r "../../$target_zip_name" ./*
		cd - >/dev/null
	done
}

function regularzip {
	if [[ $# -lt 1 ]]; then
		return
	fi
	for folder in $(find "./bin/release/$1" -mindepth 1 -type d); do
		if [[ ! -d "$folder" ]]; then
			continue
		fi
		name=$(basename "$folder")
		echo "Zipping $name..."
		target_zip_name=""
		case "$name" in
			"win-x86") target_zip_name="UnattendGen-x86-Windows.zip" ;;
			"win-x64") target_zip_name="UnattendGen-x64-Windows.zip" ;;
			"win-arm64") target_zip_name="UnattendGen-arm64-Windows.zip" ;;
			"osx-x64") target_zip_name="UnattendGen-x64-MacOS.zip" ;;
			"osx-arm64") target_zip_name="UnattendGen-arm64-MacOS.zip" ;;
			"linux-x64") target_zip_name="UnattendGen-x64-Linux.zip" ;;
			"linux-arm64") target_zip_name="UnattendGen-arm64-Linux.zip" ;;
		esac
		cd "$folder" || exit
		zip -r "../$target_zip_name" ./*
		cd - >/dev/null
	done
}

clear

echo "UnattendGen Release Publish (Unix systems)"
echo "(c) 2025. CodingWonders Software"

# Publish Script Options
# ------------------------------------------------------------
# - This option specifies whether to disable dotnet publish telemetry.
#   - Set to 1 to disable telemetry
#   - Set to 0 to enable telemetry
DOTNET_CLI_TELEMETRY_OPTOUT=1
# - This option adds compatibility for Intel (x64) releases of macOS
#   - Set to 1 to add compatibility
#   - Set to 0 to remove compatibility
#   After .NET either stops supporting Intel systems or macOS Tahoe, this option will be disabled
UNATTENDGEN_ADD_OSX_X64_COMPAT=1
# - This option controls execution on Windows Subsystem for Linux instances
#   - Set to 2 to forbid builds on WSL
#   - Set to 1 to ask the user whether to build on WSL
#   - Set to 0 to allow builds on WSL
UNATTENDGEN_CONTROL_WSL=1

WINBUILDALLOWPREFERENCE=""
case $UNATTENDGEN_CONTROL_WSL in
	0) WINBUILDALLOWPREFERENCE="Allow" ;;
	1) WINBUILDALLOWPREFERENCE="Ask" ;;
	2) WINBUILDALLOWPREFERENCE="Block" ;;
esac

echo -e "\nOPTIONS:"
echo "- Opt out of dotnet telemetry: $DOTNET_CLI_TELEMETRY_OPTOUT"
echo "- Compile build for Intel macOS: $UNATTENDGEN_ADD_OSX_X64_COMPAT"
echo "- Build policy for WSL: $UNATTENDGEN_CONTROL_WSL ($WINBUILDALLOWPREFERENCE)"
echo -e "\n"

if [[ $UNATTENDGEN_CONTROL_WSL -gt 0 ]]; then
	if [[ "$(env | grep -e "^WSL" | wc -l)" -gt 0 ]]; then
		# Running on a Windows environment
		case $UNATTENDGEN_CONTROL_WSL in
			1) 
				echo "You are running this script in a Windows environment. It is recommended that you use the Batch script (publish.bat) instead for a more native experience."
				read -p "Continue? (Y/N)" -sn1 option
				if [[ "$option" != "Y" ]]; then
					clear
					exit
				fi
				;;
			2)
				echo "You cannot run this script in a Windows environment. Please use the Batch script instead."
				exit 
				;;
		esac
		echo -e "\n"
	fi
fi

echo "Publishing self-contained binaries..."

# Publish self-contained binaries
for architecture in win-x86 win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64; do
	echo "------------------------ Building self-contained binary for target $architecture ------------------------"
	if [ "$architecture" == "osx-x64" ] && [ $UNATTENDGEN_ADD_OSX_X64_COMPAT -eq 0 ]; then
		echo "Platform $architecture not supported by publish flags."
		echo "Set the UNATTENDGEN_ADD_OSX_X64_COMPAT option in this script to 1 to enable Intel builds of UnattendGen for macOS."
		echo -e "\nAfter a .NET version either stops supporting Intel systems or macOS Tahoe, both this flag and the $architecture target will be removed from this script."
		continue
	fi
	dotnet publish -r:$architecture --sc -o "bin/release/$framework/sc/$architecture" --framework $framework
done

selfcontainedzip $framework
rm -rf "bin/release/$framework/sc" 2>/dev/null

echo "Publishing regular binaries..."

# Publish regular binaries
for architecture in win-x86 win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64; do
	echo "------------------------ Building regular binary for target $architecture ------------------------"
	if [ "$architecture" == "osx-x64" ] && [ $UNATTENDGEN_ADD_OSX_X64_COMPAT -eq 0 ]; then
		echo "Platform $architecture not supported by publish flags."
		echo "Set the UNATTENDGEN_ADD_OSX_X64_COMPAT option in this script to 1 to enable Intel builds of UnattendGen for macOS."
		echo -e "\nAfter a .NET version either stops supporting Intel systems or macOS Tahoe, both this flag and the $architecture target will be removed from this script."
		continue
	fi
	dotnet publish -r:$architecture --framework $framework
	rm -rf "bin/release/$framework/$architecture/publish"
done

regularzip $framework