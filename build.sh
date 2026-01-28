#!/usr/bin/bash

# run e.g. as: while true ; do inotifywait -e close_write *.md ; clear ; ./build.sh ; done

set -eux -o pipefail

podman run --rm -v $PWD:/home/marp/app/ -e LANG=$LANG docker.io/marpteam/marp-cli -o- talk-introduction-to-property-based-testing.md > talk-introduction-to-property-based-testing.html
