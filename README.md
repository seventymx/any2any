<div align="center" style="display: flex; justify-content: center; align-items: center;">
  <a href="https://github.com/seventymx/any2any/blob/main/LICENSE"><img src="https://img.shields.io/github/license/seventymx/any2any?style=for-the-badge&color=important"></a>
</div>

## Any2Any

Any2Any is a dynamic mapping tool designed to bridge export formats (CSV/Excel) from one system to the import formats of another, enabling seamless integration between diverse business services.

## Development Environment

```sh
# Enter the Nix shell
nix develop

# Open the project in Visual Studio Code
# **Note:** Configuration files like `.prettierrc.json` are located in the root directory.
# Ensure you open the root directory in your editor for the configurations to apply correctly.
code .

# Navigate to the backend directory
cd backend

# Start the backend server - Prototype CLI tool
dotnet run

# Open the project in JetBrains Rider
riderdev
```

The application uses SQLite as its database, combined with EF Core (code-first) for schema management. The database is created and seeded automatically when the application starts.

### Unix Development

```sh
# Install Nix (multi-user)
sh <(curl -L https://nixos.org/nix/install) --daemon

# Enable Nix flakes and set trusted users
cat <<EOF | sudo tee -a /etc/nix/nix.conf > /dev/null
experimental-features = nix-command flakes
trusted-users = root $(whoami)
EOF

# Restart the nix-daemon to apply the changes
sudo systemctl restart nix-daemon

# Add alias to start rider with the current context
# **Note:** This assumes you have JetBrains Rider installed and you use the Bash shell
echo 'alias riderdev="rider . > /dev/null 2>&1 &"' >> ~/.bash_aliases
```

### Windows Development

Nix is not natively supported on Windows. To develop Any2Any on Windows, we suggest the following options:

**Use Windows Subsystem for Linux (WSL2):**

Install WSL2 and set up a Linux distribution like Ubuntu 24.04 to enable a development environment that mimics a native Linux system.
You can use the VS Code Remote - WSL extension to open the project in a Linux environment.
Ensure you start the VS Code server from the WSL terminal to access the Nix development shell.
Alternatively, you can install JetBrains Rider inside WSL and run it via the X11 bridge by starting Rider with the configured alias from your WSL terminal.
Rider also supports WSL and offers various remote development features, but the main challenge is ensuring the Nix environment variables are correctly applied within the IDE when developing inside WSL.

**Switch to macOS or Linux:**

For a seamless development experience, consider developing on a macOS or Linux machine where Nix is fully supported.

**Manage the project dependencies manually:**

If you prefer to develop on Windows without WSL, you can manage the project dependencies manually. This approach requires you to install the necessary tools and packages on your Windows machine.

**Note:** For better reproducibility consider creating a Chocolatey package for the project dependencies.

### Entity Framework Core Migrations

To create a new migration, run the following command replacing `InitialCreate` with the desired migration name:

```sh
# Make sure your working directory is the backend project
cd backend

dotnet ef migrations add InitialCreate

# List all migrations
dotnet ef migrations list
```

## Building the Project with Nix

To build the project using Nix, you can run the `./build.sh` script.

**Note:** Disabling the sandbox is necessary for this build because the `dotnet` command needs network access to download NuGet packages.
While this approach works for now, we are exploring more elegant solutions to handle dependencies in a sandboxed environment.
