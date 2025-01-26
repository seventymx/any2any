/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.

  Author: Steffen70 <steffen@seventy.mx>
  Creation Date: 2024-07-25

  Contributors:
  - Contributor Name <contributor@example.com>
*/

{
  description = "A development environment for working with C#.";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    { self, nixpkgs, ... }@inputs:
    inputs.flake-utils.lib.eachDefaultSystem (
      system:
      let
        unstable = import nixpkgs { inherit system; };

        buildDependencies = [
          unstable.dotnet-sdk_8
        ];

        licenseHeader = ''
          <<EOL
          ===================================================================
          This Source Code Form is subject to the terms of the Mozilla Public
          License, v. 2.0. If a copy of the MPL was not distributed with this
          file, You can obtain one at https://mozilla.org/MPL/2.0/.
          ===================================================================
          EOL
        '';
      in
      {
        devShell = unstable.mkShell {
          buildInputs = [
            unstable.nixfmt-rfc-style
            unstable.yarn
            unstable.dotnet-ef
          ] ++ buildDependencies;

          shellHook = ''
            cat ${licenseHeader}
          '';
        };

        packages.backend = unstable.stdenv.mkDerivation {
          pname = "Any2Any";
          version = "0.1.0";

          src = ./backend;

          buildInputs = buildDependencies;

          configureNuget = ''
            # Clear NuGet cache
            dotnet nuget locals all --clear

            # Restore NuGet packages
            dotnet restore
          '';

          buildPhase = ''
            # Build the project
            dotnet publish -c Release -o $out --no-restore
          '';

          meta = with nixpkgs.lib; {
            description = ''
              Any2Any is a versatile mapping tool designed to dynamically connect and translate
              export formats (CSV/Excel) from one system to the import formats of another,
              enabling seamless integration between diverse business service interfaces.
            '';
            license = licenses.unfree;
            maintainers = with maintainers; [ steffen70 ];
          };
        };
      }
    );
}
