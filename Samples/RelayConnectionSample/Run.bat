set bcgService = "BackgroundService\bin\BackgroundService.exe"
set fntService = "FrontendService\bin\FrontendService.exe"

if NOT EXIST %bcgService% (
  echo Build 'BackgroundService' project first.
  goto :eof
)

if NOT EXIST %fntService% (
  echo Build 'FrontendService' project first.
  goto :eof
)

start %bcgService%
timeout 1>nul
start %fntService%
timeout 1>nul
start "FrontendService\Site\Index.html"