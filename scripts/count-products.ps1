$remoteConnString = "Server=tcp:db52374.databaseasp.net,1433;Database=db52374;User Id=db52374;Password=tS+3-7Qn4Nr?;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;"
try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($remoteConnString)
    $conn.Open()
    Write-Host "Successfully connected to remote SQL Server database!"
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM Products"
    $totalCount = $cmd.ExecuteScalar()
    Write-Host "Total products: $totalCount"
    
    $cmd.CommandText = "SELECT COUNT(*) FROM Products WHERE SKU LIKE 'ELG-%'"
    $elgCount = $cmd.ExecuteScalar()
    Write-Host "Elghazawy products: $elgCount"
    
    $conn.Close()
} catch {
    Write-Host "Failed to connect to remote database: $_"
}
