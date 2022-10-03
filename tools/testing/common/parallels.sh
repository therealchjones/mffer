#!/bin/sh

# abstraction layer over Parallels Desktop Pro command-line tools

# This script should be "source"d rather than run. It should include functions
# that make use of the `prlctl`, `prl_disk_tool`, `prlsrvctl`, and associated
# tools to manage Parallels Desktop and Parallels virtual machines on macOS. It
# should not expect to have knowledge of the operating systems running on any of
# the Parallels virtual machines other than that obtainable via the Parallels
# command-line tools themselves.
