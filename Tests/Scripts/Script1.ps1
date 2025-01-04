function Calculate-Sum {
	param (
		[int]$a,
		[int]$b
	)
	return $a + $b
}

Write-Host "Hello World. This is run when the first user logs on"
Calculate-Sum -a 184 -b 8         # This should return 192