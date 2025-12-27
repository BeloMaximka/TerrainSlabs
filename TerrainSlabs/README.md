# Regex to clean localization files

`	"(?!block-(?:soil-|gravel-|sand-|rawclay-|cob-|forestfloor-|peat-|devastatedsoil-|glacierice|muddygravel|packedglacierice|sandwavy-|snowblock))(?:\\.|[^"\\])*":\s*"(?:\\.|[^"\\])*",?
`

# TODO

# Bugs

- Panning drops from other mods like Better Ruins are not added to terrain slabs (had to use "copy" operation to avoid JSON patching errors)
