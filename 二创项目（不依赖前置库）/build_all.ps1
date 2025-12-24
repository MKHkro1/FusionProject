# 二创项目统一构建脚本
# 构建所有项目并将DLL复制到.release对应文件夹

$ErrorActionPreference = "Continue"
$baseDir = $PSScriptRoot
$releaseDir = Join-Path $baseDir ".release"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "开始构建二创项目（不依赖前置库）" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 项目与release文件夹的映射关系
$projectMapping = @{
    "GoldImitater.BepInEx" = @{
        ProjectPath = "GoldImitater.BepInEx\GoldImitater.BepInEx.csproj"
        DllName = "GoldImitater.BepInEx.dll"
        ReleaseFolder = "黄金模仿者（不依赖前置库）"
    }
    "HeiTa.BepInEx" = @{
        ProjectPath = "HeiTa\HeiTa.BepInEx.csproj"
        DllName = "HeiTa.dll"
        ReleaseFolder = "黑塔（不依赖前置库）"
    }
    "SuperGoldPresent.BepInEx" = @{
        ProjectPath = "SuperGoldPresent\SuperGoldPresent.BepInEx.csproj"
        DllName = "SuperGoldPresent.BepInEx.dll"
        ReleaseFolder = "贪欲盒子（不依赖前置库）"
    }
    "WaterPot.BepInEx" = @{
        ProjectPath = "WaterPot\WaterPot.BepInEx.csproj"
        DllName = "WaterPot.dll"
        ReleaseFolder = "水盆（不依赖前置库）"
    }
    "Wish.BepInEx" = @{
        ProjectPath = "Wish\Wish.BepInEx.csproj"
        DllName = "Wish.BepInEx.dll"
        ReleaseFolder = "纠缠之缘（动画版，不依赖前置库）"
    }
    "Wish.BepInEx.NoVideo" = @{
        ProjectPath = "Wish（无视频版本）\Wish.BepInEx.csproj"
        DllName = "Wish.BepInEx.dll"
        ReleaseFolder = "纠缠之缘（无动画版，不依赖前置库）"
    }
}

$successCount = 0
$failCount = 0

foreach ($project in $projectMapping.GetEnumerator()) {
    $projectName = $project.Key
    $projectInfo = $project.Value
    $projectPath = Join-Path $baseDir $projectInfo.ProjectPath
    
    Write-Host "`n----------------------------------------" -ForegroundColor Yellow
    Write-Host "构建项目: $projectName" -ForegroundColor Yellow
    
    if (-not (Test-Path $projectPath)) {
        Write-Host "  [错误] 项目文件不存在: $projectPath" -ForegroundColor Red
        $failCount++
        continue
    }
    
    # 构建项目
    $buildResult = dotnet build $projectPath -c Release 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [成功] 构建完成" -ForegroundColor Green
        
        # 查找输出的DLL
        $projectDir = Split-Path $projectPath -Parent
        $dllPath = Join-Path $projectDir "bin\Release\net6.0\$($projectInfo.DllName)"
        
        if (-not (Test-Path $dllPath)) {
            $dllPath = Join-Path $projectDir "bin\Release\$($projectInfo.DllName)"
        }
        
        if (Test-Path $dllPath) {
            # 复制到release文件夹
            $targetDir = Join-Path $releaseDir $projectInfo.ReleaseFolder
            
            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
                Write-Host "  [信息] 创建目录: $targetDir" -ForegroundColor Cyan
            }
            
            $targetPath = Join-Path $targetDir $projectInfo.DllName
            Copy-Item $dllPath $targetPath -Force
            Write-Host "  [成功] DLL已复制到: $targetPath" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host "  [警告] 找不到输出DLL: $dllPath" -ForegroundColor Yellow
            $failCount++
        }
    } else {
        Write-Host "  [失败] 构建失败" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Red
        $failCount++
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "构建完成!" -ForegroundColor Cyan
Write-Host "成功: $successCount 个项目" -ForegroundColor Green
Write-Host "失败: $failCount 个项目" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host "========================================" -ForegroundColor Cyan
