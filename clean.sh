#!/bin/bash
# This cleans build output, which is useful because msbuild is hot trash and can't do this by itself.
rm -rf bin */obj .vs obj
