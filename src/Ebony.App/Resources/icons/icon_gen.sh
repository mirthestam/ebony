for s in 32 64 128 256 512; do
  inkscape nl.mirthestam.ebony.svg --export-type=png --export-width=$s --export-filename=icon_$s.png
done

inkscape nl.mirthestam.ebony-symbolic.svg --export-type=png --export-width=16 --export-filename=icon_16.png

