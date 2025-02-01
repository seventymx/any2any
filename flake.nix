/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.

  Author: Steffen70 <steffen@seventy.mx>
  Creation Date: 2025-01-26

  Contributors:
  - Contributor Name <contributor@example.com>
*/

{
  description = "A development environment for working with C#, Vue.js and gRPC.";

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
          unstable.protobuf
          unstable.powershell
          unstable.yarn
          unstable.protoc-gen-js
          unstable.protoc-gen-grpc-web
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

        certificateSettings = ''
          {
            "path": "cert/localhost",
            "password": "Catapult0-Carpool4"
          }
        '';
      in
      {
        devShell = unstable.mkShell {
          buildInputs = [
            unstable.nixfmt-rfc-style
            unstable.dotnet-ef
          ] ++ buildDependencies;

          shellHook = ''
            cat ${licenseHeader}

            export VUE_APP_PORT=8443
            export API_PORT=8444
            export CERTIFICATE_SETTINGS='${certificateSettings}'
          '';
        };

        # TODO: Replace backend package output with a Docker image containing the .NET build and Vue build in wwwroot, and enable static file serving in the pipeline.
        packages.backend = unstable.stdenv.mkDerivation {
          pname = "Any2Any";
          version = "0.1.0";

          src = ./.;

          buildInputs = buildDependencies;

          configureNuget = ''
            # Navigate to the project directory
            cd backend

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
