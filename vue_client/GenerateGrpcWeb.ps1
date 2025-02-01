<#
This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

Author: Steffen70 <steffen@seventy.mx>
Creation Date: 2025-02-01

Contributors:
- Contributor Name <contributor@example.com>
#>

param (
    [string]$ProtoFilesPath,
    [string]$OutputPath
)

$directoryPath = (Resolve-Path -Relative $PSScriptRoot)

if (-not $ProtoFilesPath) {
    $ProtoFilesPath = Resolve-Path (Join-Path $directoryPath "../protos")
}

if (-not $OutputPath) {
    $OutputPath = Join-Path $directoryPath "generated"
}

if (-not (Test-Path -Path $OutputPath -PathType Container)) {
    New-Item -ItemType Directory -Path $OutputPath
}
else {
    Get-ChildItem -Path $OutputPath -Recurse | Remove-Item -Force
}

$OutputPath = Resolve-Path $OutputPath
$protoFiles = Get-ChildItem -Path $ProtoFilesPath -Filter *.proto

foreach ($protoFile in $protoFiles) {
    $protocCommand = "protoc --proto_path=$ProtoFilesPath --js_out=import_style=commonjs:$OutputPath --grpc-web_out=import_style=typescript,mode=grpcwebtext:$OutputPath $($protoFile.FullName)"

    Invoke-Expression $protocCommand
}

Write-Host "gRPC-Web TypeScript client code generation completed."
