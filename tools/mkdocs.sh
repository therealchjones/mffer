#!/bin/sh

# Build the documentation from source files, for testing locally or for
# hosting on ReadTheDocs.io. This file must reside in the
# <workspace_root>/tools/ directory to work properly, and must be run
# from the <workspace_root> directory.
#

usage() {
	cat <<"EOF"
Usage: sh tools/mkdocs.sh [--prebuild]

Options:
  --prebuild	set up everything to be built, such as on rtd.io, then exit

Note that tools/mkdocs.sh must be run from the workspace/repository root.
EOF
}

if [ "$#" -gt 1 ]; then
	usage >&2
elif [ "--prebuild" = "$1" ]; then
	PREBUILD_ONLY="Y"
elif [ "-h" = "$1" ] || [ "--help" = "$1" ]; then
	usage
	exit 0
elif [ "$#" -gt 0 ]; then
	usage >&2
	exit 1
fi

# Prints an absolute pathname corresponding to directory $1, creating the
# directory if it does not exist.
setdir() {
	if [ "$#" != "1" ]; then
		echo 'Usage: setdir dirname' >&2
		exit 1
	fi
	if [ -e "$1" ]; then
		if [ -L "$1" ]; then
			if ! (cd "$1" >&2); then
				echo "'$1' exists and is not a directory." >&2
				exit 1
			fi
		elif [ ! -d "$1" ]; then
			echo "'$1' exists and is not a directory." >&2
			exit 1
		fi
	else
		mkdir -p "$1" || exit 1
	fi
	(cd "$1" && pwd "$1") || exit 1
}

TOOLSDIR="$(setdir "$(dirname "$0")")"
ROOTDIR="$(setdir "$TOOLSDIR/..")"
DOCDIR="$(setdir "$ROOTDIR/docs")"
CONFIGDIR="$(setdir "$TOOLSDIR")"
BUILDDIR="$(setdir "$ROOTDIR/build")"
SRCDIR="$(setdir "$ROOTDIR/src")"
if [ ! -e "$ROOTDIR/mffer.csproj" ] \
	|| [ ! -e "$DOCDIR/index.rst" ] \
	|| [ ! -e "$TOOLSDIR/requirements.txt" ] \
	|| [ ! -e "$SRCDIR/Program.cs" ] \
	|| [ ! -e "$CONFIGDIR/conf.py" ]; then
	echo "Directory structure of this project is unexpected. Exiting." >&2
	exit 1
fi

set -e

mkdir -p "$BUILDDIR"/doxygen "$BUILDDIR"/sphinx

if [ "True" = "$READTHEDOCS" ]; then # running on the ReadTheDocs servers
	mv "$CONFIGDIR/conf.py" "$DOCDIR"/conf.py
	PREBUILD_ONLY=Y
fi

# Dumb workaround for https://github.com/doxygen/doxygen/issues/9362
SRCDIR="$(echo "$SRCDIR" | sed 's/\//\\\//g')"
sed "s/\$(SRCDIR)/$SRCDIR/g" "$CONFIGDIR"/Doxyfile \
	| DOXYGEN_OUTPUT="$(setdir "$BUILDDIR/doxygen")" doxygen -

if [ "Y" = "$PREBUILD_ONLY" ]; then
	exit 0
fi

sphinx-build -a -b dirhtml -n -c "$CONFIGDIR" "$DOCDIR" "$BUILDDIR/sphinx"
