from swebench.harness.constants.javascript import *
from swebench.harness.constants.python import *

TEST_JELLYFIN = 'dotnet test Jellyfin.sln --verbosity detailed --framework=net8.0 --results-directory ./test_results --logger trx -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903 --filter'
TEST_STACKEXCHANGE_REDIS = 'dotnet test tests/StackExchange.Redis.Tests/StackExchange.Redis.Tests.csproj -c Release --results-directory ./test_results --logger trx -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1504 --filter '
TEST_DOTNET_BENCHMARK = 'bash build/build.sh unit-tests && mkdir -p ./test_results && cp /testbed/TestResults/*.trx ./test_results/'
TEST_CHOCOLATEY_CHOCO = "./build.sh --target=Test --verbosity=diagnostic && mkdir -p ./test_results && cp /testbed/code_drop/TestResults/NUnit/TestResult.xml ./test_results/"
TEST_ARDALIS_CLEAN_ARCH = "dotnet test ./Clean.Architecture.sln --configuration Release --results-directory ./test_results --logger trx -p:TreatWarningsAsErrors=false"
TEST_CSVHELPER = 'dotnet test --nologo --framework=net6.0 --results-directory ./test_results --logger trx --filter'
TEST_JSONCONSOLEAPP = "dotnet test --nologo --framework=net7.0 --results-directory ./test_results --logger trx --filter"
TEST_ASPNET_BOILERPLATE = 'dotnet test --no-build --verbosity normal --results-directory ./test_results --logger trx --filter'
TEST_BITWARDEN_SERVER = 'dotnet test ./test --configuration Debug --results-directory ./test-results --logger trx --filter'
TEST_GUI_TERMINAL = 'dotnet test --verbosity normal --settings UnitTests/coverlet.runsettings --results-directory ./test_results --logger trx --filter'
TEST_FAST_ENDPOINTS = 'dotnet test FastEndpoints.sln --configuration Release --verbosity minimal --results-directory ./test_results --logger trx --filter'
TEST_DAPPERLIB = 'dotnet test tests/Dapper.Tests/Dapper.Tests.csproj -c Release --results-directory ./test_results --logger trx --filter'
TEST_MESSAGE_PACK_CSHARP = 'dotnet test -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1902 --results-directory ./test_results --logger trx --filter'
TEST_ANT_DESIGN = 'dotnet test ./tests/AntDesign.Tests/AntDesign.Tests.csproj --results-directory ./test_results --logger trx --filter'
TEST_SPECTRE_CONSOLE = 'dotnet test src/Spectre.Console.sln --results-directory ./test_results --logger trx --filter'
TEST_APP_POLLY = ' dotnet test Polly.sln --verbosity detailed --framework=net8.0 --results-directory ./test_results --logger trx --filter'

SPECS_JELLYFIN = {
    "0.1": {
        "python": "3.12",
        "test_cmd": TEST_JELLYFIN,
        "pre_install": ["dotnet restore -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903"],
        "dotnet": {
            "default": "8.0.100",
            "10345": "7.0.100",
            "9312": "7.0.100",
            "8147": "7.0.100",
            "8104": "7.0.100",
        },
        "install": "dotnet build -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903"
    },
    "0.2": {
        "python": "3.12",
        "test_cmd": "dotnet test Jellyfin.sln --verbosity detailed --results-directory ./test_results --logger trx -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903 --filter",
        "pre_install": ["dotnet restore -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903"],
        "dotnet": {
            "default": "8.0.100",
            "10345": "7.0.100",
            "9312": "7.0.100",
            "8147": "7.0.100",
            "8104": "7.0.100",
        },
        "install": "dotnet build -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903"
    }
    
    
}

SPECS_STACKEXCHANGE_REDIS = {
    k: {
        "test_cmd": TEST_STACKEXCHANGE_REDIS,
        "dotnet": {
            "default": "8.0.404"
        },
        "install": "dotnet build Build.csproj -c Release /p:CI=true -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1504",
        "python": "3.12",
        "pre_install": ["dotnet restore Build.csproj -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1504"]
    }
    for k in ["0.1"]
}

SPECS_DOTNET_BENCHMARK = {
    "0.1": {
        "python": "3.12",
        "test_cmd": TEST_DOTNET_BENCHMARK,
        "pre_install": ["bash build/build.sh restore"],
        "dotnet": {
            "default": "8.0.200",
        },
        "install": "bash build/build.sh"
    },
    "0.2": {
        "python": "3.12",
        "test_cmd": 'bash build.sh --target FastTests && mkdir -p ./test_results && cp /testbed/TestResults/*.trx ./test_results/',
        "pre_install": ["bash build.sh --target Restore"],
        "dotnet": {
            "default": "8.0.200",
        },
        "install": "bash build.sh --target Build",
    },
    "0.3": {
        "python": "3.12",
        "test_cmd": 'dotnet test tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj --configuration Release && mkdir -p ./test_results && cp /testbed/TestResults/*.trx ./test_results/',
        "pre_install": ["bash build.sh --target Restore"],
        "dotnet": {
            "default": "7.0.100",
        },
        "install": "bash build.sh --target Build",
    },
    "0.4": {
        "python": "3.12",
        "test_cmd": 'dotnet test tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj --configuration Release --results-directory ./test_results --logger trx',
        "pre_install": ["dotnet restore tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj"],
        "dotnet": {
            "default": "8.0.200",
        },
        "install": "dotnet build tests/BenchmarkDotNet.IntegrationTests/BenchmarkDotNet.IntegrationTests.csproj",
    },
    "0.5": {
        "python": "3.12",
        "test_cmd": 'dotnet test tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj  --configuration Release --results-directory ./test_results --logger trx',
        "pre_install": ["dotnet restore BenchmarkDotNet.sln"],
        "dotnet": {
            "default": "8.0.200",
        },
        "install": "dotnet build BenchmarkDotNet.sln",
    }
}

SPECS_CHOCOLOATEY_CHOCO = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_CHOCOLATEY_CHOCO,
        "pre_install": [
            "sed -i 's/if (source is null)/if (source == null)/' src/chocolatey/infrastructure.app/nuget/ChocolateyNugetCredentialProvider.cs",
            "sed -i 's/if (source is null)/if (source == null)/' src/chocolatey/infrastructure.app/nuget/ChocolateyNugetCredentialProvider.cs",
        ],
        "dotnet": {
            "default": "6.0.100",
        },
        "install": "./build.sh --verbosity=diagnostic --target=Build"
    }
    for k in ["0.1"]
}

SPECS_ARDALIS_CLEAN_ARCH = {
    k: {
        "test_cmd": TEST_ARDALIS_CLEAN_ARCH,
        "dotnet": {
            "default": "9.0.100-rc.2.24474.11",
            "805": "8.0.100",
        },
        "install": "dotnet build --configuration Release -p:TreatWarningsAsErrors=false",
        "python": "3.12",
        "pre_install": ["dotnet restore -p:TreatWarningsAsErrors=false"],
    }
    for k in ["0.1"]
}

SPECS_CSVHELPER = {
    k: {
        "python": "3.12",
        "dotnet": {
            "default": "6.0.100"
        },
        "pre_install": [
            "sed -i '/<PropertyGroup>/a\\    <Nullable>enable</Nullable>\\n    <LangVersion>11.0</LangVersion>' /testbed/src/CsvHelper/CsvHelper.csproj",
            "sed -i '/<PropertyGroup>/a\\    <LangVersion>11.0</LangVersion>' /testbed/tests/CsvHelper.Tests/CsvHelper.Tests.csproj",
            "sed -i '/<TargetFrameworks>/c\\    <TargetFrameworks>net6.0</TargetFrameworks>' /testbed/src/CsvHelper/CsvHelper.csproj",
            "sed -i '/<ItemGroup Condition/,/<\\/ItemGroup>/d' /testbed/src/CsvHelper/CsvHelper.csproj",
            "dotnet restore"
        ],
        "install": "dotnet build",
        "test_cmd": TEST_CSVHELPER,
    }
    for k in ["0.1"]
}

SPECS_JSONCONSOLEAPP = {
    k: {
        "python": "3.12",
        "dotnet": {
            "default": "7.0.100"
        },
        "pre_install": ["dotnet restore"],
        "install": "dotnet build",
        "test_cmd": TEST_JSONCONSOLEAPP,
    }
    for k in ["0.1"]
}

SPECS_ASPNET_BOILERPLATE = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_ASPNET_BOILERPLATE,
        "pre_install": ["dotnet restore"],
        "dotnet": {
            "default": "9.0.100-rc.2.24474.11",
            "7016": "8.0.100-rc.1.23463.5",
            "6988": "8.0.100-rc.1.23463.5",
            "6960": "8.0.100-rc.1.23463.5",
            "6956": "8.0.100-rc.1.23463.5",
            "6952": "8.0.100-rc.1.23463.5",
            "6941": "8.0.100-rc.1.23463.5",
            "6893": "8.0.100-rc.1.23463.5",
            "6891": "8.0.100-rc.1.23463.5",
            "6883": "8.0.100-rc.1.23463.5",
            "6871": "8.0.100-rc.1.23463.5",
            "6824": "8.0.100-rc.1.23463.5",
            "6811": "8.0.100-rc.1.23463.5",
            "6805": "8.0.100-rc.1.23463.5",
            "6804": "8.0.100-rc.1.23463.5",
            "6796": "8.0.100-rc.1.23463.5",
            "6791": "7.0.100",
            "6789": "7.0.100",
            "6760": "7.0.100",
            "6754": "7.0.100",
            "6730": "7.0.100",
            "6679": "8.0.100-rc.1.23463.5",
            "6657": "7.0.100",
            "6648": "7.0.100",
            "6621": "7.0.100",
            "6573": "6.0.100",
            "6567": "6.0.100",
            "6519": "6.0.100",
            "6501": "6.0.100",
            "6444": "6.0.100",
            "6394": "6.0.100",
            "6377": "6.0.100",
            "6358": "6.0.100",
        },
        "install": "dotnet build"
    }
    for k in ["0.1"]
}

SPECS_BITWARDEN = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_BITWARDEN_SERVER,
        "pre_install": ["dotnet restore"],
        "dotnet": {
            "default": "8.0.100",
        },
        "install": "dotnet build"
    }
    for k in ["0.1"]
}

SPECS_GUI_TERMINAL = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_GUI_TERMINAL,
        "pre_install": ["dotnet restore"],
        "dotnet": {
            "default": "8.0.404",
            "3591": "7.0.200",
            "3195": "7.0.200",
            "2924": "7.0.200",
        },
        "install": "dotnet build"
    }
    for k in ["0.1"]
}

SPECS_FAST_ENDPOINTS = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_FAST_ENDPOINTS,
        "pre_install": ["dotnet restore"],
        "dotnet": {
            "default": "6.0.404",
        },
        "install": "dotnet build"
    }
    for k in ["0.1"]
}

SPECS_DAPPERLIB = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_DAPPERLIB,
        "pre_install": ["dotnet restore Dapper.sln"],
        "dotnet": {
            "default": "8.0.404",
            "2022": "8.0.100",
            "2144": "9.0.101",
        },
        "install": "dotnet build Dapper.sln"
    }
    for k in ["0.1", "2.1.15"]
}


SPECS_MESSAGE_PACK_CSHARP = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_MESSAGE_PACK_CSHARP,
        "pre_install": ["dotnet restore -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1902"],
        "dotnet": {
            "default": "8.0.404",
            "1962": "8.0.300",
            "1960": "8.0.300",
            "1946": "8.0.300",
            "1932": "8.0.300",
            "1926": "8.0.300",
            "1910": "8.0.300",
            "1907": "8.0.300",
            "1905": "8.0.300",
            "1898": "8.0.300",
            "1893": "8.0.300",
            "1873": "8.0.300",
            "1859": "8.0.300",
            "1842": "8.0.300",
            "1824": "8.0.201",
            "1818": "8.0.201",
            "1798": "8.0.201",
            "1754": "8.0.100",
            "1743": "8.0.100",
            "1736": "8.0.100",
        },
        "install": "dotnet build -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1902"
    }
    for k in ["0.1", "v3.0.238-rc.1"]
}

SPECS_ANT_DESIGN = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_ANT_DESIGN,
        "pre_install": [
            "apt-get update",
            "apt-get install -y wget",
            "wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb",
            "dpkg -i packages-microsoft-prod.deb",
            "rm packages-microsoft-prod.deb",
            "apt-get update",
            "apt-get install -y dotnet-sdk-8.0",
            "apt-get install -y nodejs npm",
            "dotnet restore"
        ],
        "dotnet": {
            "default": "9.0.100",
        },
        "install": "npm install && dotnet build"
    }
    for k in ["0.1"]
}

SPECS_SPECTRE_CONSOLE = {
    k: {
        "python": "3.12",
        "test_cmd": TEST_SPECTRE_CONSOLE,
        "pre_install": [
            "dotnet tool restore",
        ],
        "dotnet": {
            "default": "8.0.204",
            "1338": "7.0.100",
            "1308": "7.0.100",
            "1304": "7.0.100",
            "1303": "7.0.100",
            "1215": "7.0.100",
            "1164": "7.0.100",
            "1131": "7.0.100",
            "1091": "7.0.100",
            "1086": "7.0.100",
            "1032": "7.0.100",
            "1008": "6.0.400",
            "924": "6.0.300",
            "911": "6.0.300",
            "877": "6.0.300",
        },
        "install": "dotnet build src/Spectre.Console.sln"
    }
    for k in ["0.1"]
}


SPECS_APP_POLLY = {
    "0.1": {
        "python": "3.12",
        "test_cmd": TEST_APP_POLLY,
        "pre_install": ["dotnet restore"],
        "dotnet": {
            "default": "8.0.404",
        },
        "install": "dotnet build"
    },
    "0.2": {
        "python": "3.12",
        "test_cmd":  'dotnet test Polly.sln --verbosity detailed --framework=net7.0 --results-directory ./test_results --logger trx --filter',
        "pre_install": ["dotnet restore --force"],
        "dotnet": {
            "default": "7.0.403",
        },
        "install": "dotnet build"
    },
}

SPECS_AUTOFAC = {
    "0.1": {
        "python": "3.12",
        "test_cmd":  'dotnet test Autofac.sln --verbosity detailed --results-directory ./test_results --logger trx --filter',
        "pre_install": ["sudo apt-get update && sudo apt-get install libssl-dev","dotnet restore --force"],
        "dotnet": {
            "default": "8.0.401",
            "1389": "7.0.203",
            "1366": "7.0.101",
            "1364": "7.0.101",
            "1352": "6.0.200",
            "1350": "7.0.101",
            "1321": "6.0.200",
            "1316": "6.0.200",
            "1302": "5.0.400",
            "1010": "2.2.101",
            "1008": "2.2.101",
            "977": "2.2.101",
            "961": "2.2.101",
        },
        "install": "dotnet build"
    }
}

SPECS_IMAGE_SHARP = {
    "0.1": {
        "python": "3.12",
        "test_cmd":  'dotnet test ImageSharp.sln --verbosity detailed --results-directory ./test_results --logger trx --filter',
        "pre_install": ["git submodule init", "git submodule update", "dotnet restore -r linux-x64"],
        "dotnet": {
            "default": "8.0.401",
        },
        "install": "dotnet build"
    }
}

SPECS_MOQ = {
    "0.1": {
        "python": "3.12",
        "test_cmd":  'dotnet test Moq.sln --verbosity detailed --results-directory ./test_results --logger trx --filter',
        "pre_install": [
            "wget http://archive.ubuntu.com/ubuntu/pool/main/o/openssl/libssl1.1_1.1.1f-1ubuntu2_amd64.deb",
            "dpkg -i libssl1.1_1.1.1f-1ubuntu2_amd64.deb",
            "wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb",
            "dpkg -i packages-microsoft-prod.deb",
            "apt-get update",
            "apt-get install -y dotnet-runtime-3.1",
            "dotnet restore Moq.sln"
        ],
        "dotnet": {
            "default": "6.0.300",
        },
        "install": "dotnet build Moq.sln"
    },
    "0.2": {
        "python": "3.12",
        "test_cmd":  'dotnet test Moq.sln --verbosity detailed --results-directory ./test_results --logger trx --filter',
        "pre_install": ["dotnet add /testbed/tests/Moq.Tests.FSharpTypes/Moq.Tests.FSharpTypes.fsproj package FSharp.Core --version 4.6.2", "dotnet add /testbed/tests/Moq.Tests/Moq.Tests.csproj package FSharp.Core --version 4.6.2", "dotnet restore Moq.sln"],
        "dotnet": {
            "default": "6.0.300",
        },
        "install": "dotnet build Moq.sln"
    }
}

SPECS_REST_SHARP = {
    k: {
        "python": "3.12",
        "test_cmd": "dotnet test RestSharp.sln --verbosity detailed --results-directory ./test_results --logger trx --filter",
        "pre_install": ["dotnet restore RestSharp.sln"],
        "dotnet": {
            "default": "8.0.404",
            "2022": "8.0.100",
            "2021": "7.0.100",
            "2020": "7.0.100",
            "1962": "7.0.100",
            "1676": "7.0.100",
            "2275": "9.0.100",
        },
        "install": "dotnet build RestSharp.sln"
    }
    for k in ["0.1", "2.1.15"]
}

SPECS_SERILOG = {
    k: {
        "python": "3.12",
        "test_cmd": "dotnet test Serilog.sln --verbosity detailed --results-directory ./test_results --logger trx --filter",
        "pre_install": ["dotnet restore Serilog.sln"],
        "dotnet": {
            "default": "8.0.100",
            "1955": "7.0.102",
            "1927": "7.0.102",
            "1926": "7.0.102",
            "1910": "7.0.102",
            "1908": "7.0.102",
            "1906": "7.0.102",
            "1903": "7.0.102",
            "1901": "7.0.102",
            "1179": "2.1.4",
            "1172": "2.1.4",
            "1141": "2.1.4",
            "551": "2.1.4",
            "2067": "7.0.102",
            "1897": "7.0.102",
            "1890": "7.0.102",
            "1705": "5.0.202",
        },
        "install": "dotnet build Serilog.sln"
    }
    for k in ["0.1", "2.1.15"]
}



SPECS_AVALONIA = {
    "0.1": {
        "python": "3.12",
        "test_cmd": """
            dotnet test tests/Avalonia.Base.UnitTests/Avalonia.Base.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Benchmarks/Avalonia.Benchmarks.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Build.Tasks.UnitTest/Avalonia.Build.Tasks.UnitTest.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.DesignerSupport.Tests/Avalonia.DesignerSupport.Tests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Generators.Tests/Avalonia.Generators.Tests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Headless.NUnit.UnitTests/Avalonia.Headless.NUnit.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Headless.XUnit.UnitTests/Avalonia.Headless.XUnit.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.LeakTests/Avalonia.LeakTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Markup.UnitTests/Avalonia.Markup.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.ReactiveUI.UnitTests/Avalonia.ReactiveUI.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx --filter "FullyQualifiedName!=Avalonia.Skia.UnitTests.Media.TextFormatting.TextLayoutTests.Should_Measure_TextLayoutSymbolWithAndWidthIncludingTrailingWhitespaceAndMinTextWidth";
        """,
        "pre_install": ["git submodule update --init --recursive", "dotnet workload install android wasm-tools", "curl -sSL https://raw.githubusercontent.com/Samsung/Tizen.NET/main/workload/scripts/workload-install.sh | bash"],
        "dotnet": {
            "default": "8.0.404",
        },
        "install": "dotnet build nukebuild/_build.csproj"
    },
    "0.2": {
        "python": "3.12",
        "test_cmd": """
            dotnet test tests/Avalonia.Base.UnitTests/Avalonia.Base.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Benchmarks/Avalonia.Benchmarks.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Build.Tasks.UnitTest/Avalonia.Build.Tasks.UnitTest.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.DesignerSupport.Tests/Avalonia.DesignerSupport.Tests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Generators.Tests/Avalonia.Generators.Tests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Headless.NUnit.UnitTests/Avalonia.Headless.NUnit.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Headless.XUnit.UnitTests/Avalonia.Headless.XUnit.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.LeakTests/Avalonia.LeakTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Markup.UnitTests/Avalonia.Markup.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.ReactiveUI.UnitTests/Avalonia.ReactiveUI.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx --filter "FullyQualifiedName!=Avalonia.Skia.UnitTests.Media.TextFormatting.TextLayoutTests.Should_Measure_TextLayoutSymbolWithAndWidthIncludingTrailingWhitespaceAndMinTextWidth";
        """,
        "pre_install": ["git submodule update --init --recursive", "dotnet workload install android wasm-tools", "curl -sSL https://raw.githubusercontent.com/Samsung/Tizen.NET/main/workload/scripts/workload-install.sh | bash"],
        "dotnet": {
            "default": "7.0.404",
        },
        "install": "dotnet build nukebuild/_build.csproj"
    },
    "0.3": {
        "python": "3.12",
        "test_cmd": """
            dotnet test tests/Avalonia.Base.UnitTests/Avalonia.Base.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Benchmarks/Avalonia.Benchmarks.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Build.Tasks.UnitTest/Avalonia.Build.Tasks.UnitTest.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.DesignerSupport.Tests/Avalonia.DesignerSupport.Tests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Generators.Tests/Avalonia.Generators.Tests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Headless.NUnit.UnitTests/Avalonia.Headless.NUnit.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Headless.XUnit.UnitTests/Avalonia.Headless.XUnit.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.LeakTests/Avalonia.LeakTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Markup.UnitTests/Avalonia.Markup.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.ReactiveUI.UnitTests/Avalonia.ReactiveUI.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx --filter "FullyQualifiedName!=Avalonia.Skia.UnitTests.Media.TextFormatting.TextLayoutTests.Should_Measure_TextLayoutSymbolWithAndWidthIncludingTrailingWhitespaceAndMinTextWidth";
        """,
        "pre_install": ["git submodule update --init --recursive", "dotnet workload install android wasm-tools", "curl -sSL https://raw.githubusercontent.com/Samsung/Tizen.NET/main/workload/scripts/workload-install.sh | bash"],
        "dotnet": {
            "default": "7.0.101",
        },
        "install": "dotnet build nukebuild/_build.csproj"
    },
    "0.4": {
        "python": "3.12",
        "test_cmd": """
            dotnet test tests/Avalonia.Base.UnitTests/Avalonia.Base.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Benchmarks/Avalonia.Benchmarks.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Build.Tasks.UnitTest/Avalonia.Build.Tasks.UnitTest.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.DesignerSupport.Tests/Avalonia.DesignerSupport.Tests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Generators.Tests/Avalonia.Generators.Tests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Headless.NUnit.UnitTests/Avalonia.Headless.NUnit.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Headless.XUnit.UnitTests/Avalonia.Headless.XUnit.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.LeakTests/Avalonia.LeakTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Markup.UnitTests/Avalonia.Markup.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.ReactiveUI.UnitTests/Avalonia.ReactiveUI.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx;
            dotnet test tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj --verbosity detailed --results-directory ./test_results --logger trx --filter "FullyQualifiedName!=Avalonia.Skia.UnitTests.Media.TextFormatting.TextLayoutTests.Should_Measure_TextLayoutSymbolWithAndWidthIncludingTrailingWhitespaceAndMinTextWidth";
        """,
        "pre_install": ["git submodule update --init --recursive", "dotnet workload install android wasm-tools", "curl -sSL https://raw.githubusercontent.com/Samsung/Tizen.NET/main/workload/scripts/workload-install.sh | bash"],
        "dotnet": {
            "default": "6.0.202",
        },
        "install": "dotnet build nukebuild/_build.csproj"
    }
     
}


SPECS_THREEMAMMALS_OCELOT = {
    k: {
        "python": "3.12",
        "test_cmd": "dotnet test Ocelot.sln --verbosity detailed --results-directory ./test_results --logger trx --filter",
        "pre_install": ["dotnet restore Ocelot.sln"],
        "dotnet": {
            "default": "9.0.100",
        },
        "install": "dotnet build Ocelot.sln"
    }
    for k in ["0.1"]
}

SPECS_MASSTRANSIT = {
    k: {
        "python": "3.12",
        "test_cmd": "dotnet test src/MassTransit.sln --verbosity detailed --results-directory ./test_results --logger trx --filter",
        "pre_install": ["dotnet restore src/MassTransit.sln"],
        "dotnet": {
            "default": "8.0.100",
        },
        "install": "dotnet build src/MassTransit.sln"
    }
    for k in ["0.1"]
}

SPECS_DOTNET_EFCORE = {
    "0.1": {
        "python": "3.12",
        "test_cmd": "dotnet test EFCore.sln --verbosity detailed --results-directory ./test_results --logger trx -p:WarningsNotAsErrors=NU1903 --filter",
        "pre_install": ["dotnet restore EFCore.sln -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903"],
        "dotnet": {
            "default": "8.0.100",
            "34267": "9.0.100-preview.5.24307.3",
            "33633": "9.0.100-preview.4.24267.66",
            "34052": "9.0.100-preview.5.24307.3",
            "34267": "9.0.0-preview.7.24365.1",
            "34832": "9.0.100-preview.5.24307.3",
            "33106": "8.0.101",
            "34832": "9.0.100-rc.1.24452.12",
            
        },
        "install": "dotnet build EFCore.sln -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903"
    },
    "0.2": {
        "python": "3.12",
        "test_cmd": "dotnet test All.sln --verbosity detailed --results-directory ./test_results --logger trx -p:WarningsNotAsErrors=NU1903--filter",
        "pre_install": ["dotnet restore All.sln -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903"],
        "dotnet": {
            "default": "8.0.100",
            "34267": "9.0.100-preview.5.24307.3",
            "33633": "9.0.100-preview.4.24267.66",
            "33106": "8.0.101",
            
            
        },
        "install": "dotnet build All.sln -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903"
    }
    
}


MAP_REPO_VERSION_TO_SPECS_CS = {
    "jellyfin/jellyfin": SPECS_JELLYFIN,
    "StackExchange/StackExchange.Redis": SPECS_STACKEXCHANGE_REDIS,
    "dotnet/BenchmarkDotNet": SPECS_DOTNET_BENCHMARK,
    "chocolatey/choco": SPECS_CHOCOLOATEY_CHOCO,
    "ardalis/CleanArchitecture": SPECS_ARDALIS_CLEAN_ARCH,
    "JoshClose/CsvHelper": SPECS_CSVHELPER,
    "yasharthb/JSONConsoleApp": SPECS_JSONCONSOLEAPP,
    "aspnetboilerplate/aspnetboilerplate": SPECS_ASPNET_BOILERPLATE,
    "bitwarden/server": SPECS_BITWARDEN,
    "gui-cs/Terminal.Gui": SPECS_GUI_TERMINAL,
    "FastEndpoints/FastEndpoints": SPECS_FAST_ENDPOINTS,
    "DapperLib/Dapper": SPECS_DAPPERLIB,
    "MessagePack-CSharp/MessagePack-CSharp": SPECS_MESSAGE_PACK_CSHARP,
    "ant-design-blazor/ant-design-blazor": SPECS_ANT_DESIGN,
    "spectreconsole/spectre.console": SPECS_SPECTRE_CONSOLE,
    "App-vNext/Polly": SPECS_APP_POLLY,
    "SixLabors/ImageSharp": SPECS_IMAGE_SHARP,
    "autofac/Autofac": SPECS_AUTOFAC,
    "devlooped/moq": SPECS_MOQ,
    "restsharp/RestSharp": SPECS_REST_SHARP,
    "serilog/serilog": SPECS_SERILOG,
    "AvaloniaUI/Avalonia": SPECS_AVALONIA,
    "ThreeMammals/Ocelot": SPECS_THREEMAMMALS_OCELOT,
    "MassTransit/MassTransit": SPECS_MASSTRANSIT,
    "dotnet/efcore": SPECS_DOTNET_EFCORE,
}

MAP_REPO_TO_INSTALL_CS = {}

MAP_REPO_VERSION_TO_SPECS = {
    **MAP_REPO_VERSION_TO_SPECS_JS,
    **MAP_REPO_VERSION_TO_SPECS_PY,
    **MAP_REPO_VERSION_TO_SPECS_CS,
}

MAP_REPO_TO_INSTALL = {
    **MAP_REPO_TO_INSTALL_JS,
    **MAP_REPO_TO_INSTALL_PY,
    **MAP_REPO_TO_INSTALL_CS,
}

MAP_REPO_TO_EXT = {
    **{k: "js" for k in MAP_REPO_VERSION_TO_SPECS_JS.keys()},
    **{k: "py" for k in MAP_REPO_VERSION_TO_SPECS_PY.keys()},
    **{k: "cs" for k in MAP_REPO_VERSION_TO_SPECS_CS.keys()},
}