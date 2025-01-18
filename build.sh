#!/bin/bash

NIXPKGS_ALLOW_UNFREE=1 nix build .#backend --option sandbox false --impure