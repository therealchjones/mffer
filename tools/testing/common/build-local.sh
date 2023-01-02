#!/bin/sh

sh -u '$MFFER_TEST_FRAMEWORK' || exit 1

startBuild 'local'
runBuild mffer
runBuild docs
endBuild 'local'
