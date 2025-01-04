# Write 2 functions. One that converts Celsius to Fahrenheit and another that converts kph to mph. (Yes, I used Copilot for this one)

function Convert-CelsiusToFahrenheit {
	param (
		[int]$celsius
	)
	return ($celsius * 9/5) + 32
}

function Convert-KphToMph {
	param (
		[int]$kph
	)
	return $kph * 0.621371
}

Write-Host "Hello Again. This is run when a user logs on for the first time"

Convert-CelsiusToFahrenheit -celsius 100
Convert-KphToMph -kph 100