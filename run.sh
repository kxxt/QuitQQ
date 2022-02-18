#!/bin/sh

SCRIPTPATH="$( cd -- "$(dirname "$0")" >/dev/null 2>&1 ; pwd -P )"

"$SCRIPTPATH/QuitQQ.App/bin/Release/net6.0/QuitQQ.App" > QuitQQ.log 2>&1
