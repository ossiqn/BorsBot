$AppDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$EngineDir = Join-Path $AppDir "engine"
$LogDir = Join-Path $EngineDir "logs"
$PidFile = Join-Path $EngineDir "engine.pid"

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir | Out-Null
}

function PythonBulAndKontrolEt {
    $pythonYollar = @(
        "python",
        "python3",
        "$env:LOCALAPPDATA\Programs\Python\Python311\python.exe",
        "$env:LOCALAPPDATA\Programs\Python\Python310\python.exe",
        "C:\Python311\python.exe",
        "C:\Python310\python.exe"
    )
    foreach ($yol in $pythonYollar) {
        try {
            $versiyon = & $yol --version 2>&1
            if ($versiyon -match "Python 3\.(1[0-9]|[2-9]\d)") {
                return $yol
            }
        } catch {}
    }
    return $null
}

function MotorCalisiyorMu {
    try {
        $yanit = Invoke-WebRequest -Uri "http://127.0.0.1:5000/saglik" -TimeoutSec 2 -UseBasicParsing
        return $yanit.StatusCode -eq 200
    } catch {
        return $false
    }
}

function MotorBaslat {
    param([string]$PythonYol)

    if (MotorCalisiyorMu) {
        Write-Host "[OK] Motor zaten calisiyor."
        return $true
    }

    Write-Host "[...] Zeka motoru baslatiliyor..."

    $islem = Start-Process -FilePath $PythonYol `
        -ArgumentList "-m uvicorn main:app --host 127.0.0.1 --port 5000 --log-level warning" `
        -WorkingDirectory $EngineDir `
        -WindowStyle Hidden `
        -PassThru

    $islem.Id | Out-File -FilePath $PidFile -Encoding ascii

    $deneme = 0
    while ($deneme -lt 15) {
        Start-Sleep -Milliseconds 500
        if (MotorCalisiyorMu) {
            Write-Host "[OK] Motor baslatildi. PID: $($islem.Id)"
            return $true
        }
        $deneme++
    }

    Write-Host "[HATA] Motor baslatilamadi."
    return $false
}

function MotorDurdur {
    if (Test-Path $PidFile) {
        $pid = Get-Content $PidFile
        try {
            Stop-Process -Id $pid -Force
            Remove-Item $PidFile
            Write-Host "[OK] Motor durduruldu."
        } catch {
            Write-Host "[UYARI] Motor zaten durmus."
        }
    }
}

$python = PythonBulAndKontrolEt
if (-not $python) {
    [System.Windows.Forms.MessageBox]::Show(
        "Python 3.10+ bulunamadi.`npython.org adresinden yukleyin.",
        "BorsaBot - Hata",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
    exit 1
}

$motorOk = MotorBaslat -PythonYol $python

if ($motorOk) {
    $botExe = Join-Path $AppDir "BorsaBot.exe"
    Start-Process -FilePath $botExe
} else {
    [System.Windows.Forms.MessageBox]::Show(
        "Zeka motoru baslatilamadi.`nLog dosyasini kontrol edin: $LogDir",
        "BorsaBot - Hata",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
}

Register-EngineExitHandler {
    MotorDurdur
}