makecert -r -pe -n "CN=MarsMedia" -ss CA -sr CurrentUser ^
         -a sha256 -cy authority -sky signature -sv MarsMediaCA.pvk MarsMediaCA.cer
		 
		 
 makecert -pe -n "CN=MarsMediaCode" -a sha256 -cy end ^
 -sky signature ^
 -ic MarsMediaCA.cer -iv MarsMediaCA.pvk ^
 -sv MarsMediaCodeCA.pvk MarsMediaCodeCA.cer
 
 pvk2pfx -pvk MarsMediaCodeCA.pvk -spc MarsMediaCodeCA.cer -pfx MarsMediaCodeCA.pfx