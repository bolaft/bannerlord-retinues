# Retinues

Retinues is a custom troop mod for **Mount & Blade II: Bannerlord**.

[Download on Nexus Mods](https://www.nexusmods.com/mountandblade2bannerlord/mods/8847)

## Building & Development

- **Requirements:** .NET 4.72.
- **Project Structure:**  
  - `cfg/` - Mod config files.
  - `loc/` - Localization files.
  - `src/Retinues/Core` - Main mod.
  - `src/Retinues/MCM` - Optional MCM support mod.
  - `src/PrefabBuilder/` - Tool for generating GUI prefabs from Scriban templates.
  - `tpl/` - Scriban templates and partials for GUI generation.
  - `xml/` - XML module data.

## Contributing

- **Fork the repository** and create a feature branch.
- **Test your changes** in-game before submitting a pull request.
- **Format your code** with `csharpier`.
- **Describe your changes** clearly in the PR, referencing related issues if applicable.

## Guidelines

- **Keep features modular:** Use wrappers and helpers for game objects.
- **Document intent:** All public APIs should have a summary comment explaining their purpose.
- **Prefer extension over modification:** Add new features via behaviors, patches, and mixins.

## Support

- **Bug Reports:** Use either the Nexus Mods page or GitHub Issues.
- **Feature Requests:** Use the Nexus Mods page.