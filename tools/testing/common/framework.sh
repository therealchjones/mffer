#!/bin/sh

# Ensure this variable was exported by script calling this one
[ -n "${MFFER_TEST_FRAMEWORK:=}" ] || exit 1

getCanonicalDir() {
	if [ -n "${1:-}" ] && [ -d "${1:-}" ] && (cd "${1:-}" && pwd); then
		return 0
	else
		echo "Unable to access directory '${1:-}'" >&2
		return 1
	fi
}
getOs() {
	case "$(uname -s)" in
		Darwin)
			echo "macos"
			return 0
			;;
		Linux)
			echo "linux"
			return 0
			;;
		*)
			echo "Warning: Operating system not recognized; assuming Windows" >&2
			echo "windows"
			return 0
			;;
	esac
}
getOsPlatform() {
	case "$(getOs)" in
		macos)
			echo "osx-x64"
			return 0
			;;
		linux)
			echo "linux-x64"
			return 0
			;;
		windows)
			echo "win-x64"
			return 0
			;;
		*)
			echo "Error: OS unrecognized" >&2
			return 1
			;;
	esac
}
getScript() (
	for dir in "$(getOs)" common; do
		for file in "$(getTestDir)/$dir/${1:-}.sh" "$(getTestDir)/$dir/${1:-}.bat"; do
			if [ -r "$file" ]; then
				echo "$file"
				return 0
			fi
		done
	done
	echo "Error: Unable to find a script '${1:-}'" >&2
	return 1
)
getSourceDir() {
	if ! getCanonicalDir "$(getTestDir)/../.."; then
		echo 'Error: Unable to determine source code directory' >&2
		return 1
	fi
	return 0
}
getTestDir() (
	dir="$(dirname "${0:-}")"
	while [ -n "$dir" ] && [ "$dir" != "/" ]; do
		if [ -r "$dir/test.sh" ]; then
			echo "$dir"
			return 0
		fi
		dir="$(getCanonicalDir "$dir/..")" || return 1
	done
	echo "Error: Unable to determine test directory" >&2
	return 1
)
getVersionString() (
	sourcetag=''
	sourcetag="$(git -C "$(getSourceDir)" tag --points-at)" || true
	if [ -z "$sourcetag" ] \
		|| ! isSourceTreeClean; then
		echo "prerelease"
		return 0
	elif [ "$sourcetag" = "${sourcetag#[Vv][0-9]}" ]; then
		# the HEAD tag is not a version tag
		echo "prerelease"
		return 0
	else
		echo "$sourcetag"
		return 0
	fi
)
isSourceTreeClean() (
	if ! output="$(git -C "$(getSourceDir)" status --porcelain)" \
		|| [ -n "$output" ]; then
		return 1
	fi
	return 0
)
runBuild() {
	runScript "build-${1:-}" || return 1
}
runScript() {
	sh "$(getScript "${1:-}")" || return 1
}
