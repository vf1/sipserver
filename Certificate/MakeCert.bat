"C:\Program Files\Microsoft SDKs\Windows\v6.0A\bin\makecert" -r -pe -n "CN=OfficeSIP Server" -b 01/01/2007 -e 01/01/2020 -sky exchange OfficeSIP.cer -sv OfficeSIP.pvk
"C:\Program Files\Microsoft SDKs\Windows\v6.0A\bin\pvk2pfx.exe" -pvk OfficeSIP.pvk -spc OfficeSIP.cer -pfx OfficeSIP.pfx  
