cd C:\Users\hutto\OneDrive\Documents\Git\HexWork

git pull

if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvarsall.bat" (
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvarsall.bat" x64
) else (
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
)

devenv C:\Users\hutto\OneDrive\Documents\Git\HexWork\HexWork.sln