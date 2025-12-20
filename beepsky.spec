%global debug_package %{nil}

Name:           beepsky
Version:        0.0.1
Release:        1%{?dist}
BuildArch:      x86_64
Summary:        Beepsky Discord Bot
License:        MIT
Url:            https://github.com/CorruptComputer/Beepsky

BuildRequires:  dotnet-sdk-10.0
Requires:       dotnet-runtime-10.0 libsodium libopusenc yt-dlp ffmpeg-free

%description
Beepsky is a Discord bot.

%prep
# Source already present, copy to build directory
cp -r ../../../Beepsky .

%build
cd Beepsky
ls -la
dotnet publish -c Release -f net10.0 -r linux-x64 --self-contained false

%install
%{__mkdir_p} %{buildroot}/usr/local/bin # Symlink to the binary will go here
%{__mkdir_p} %{buildroot}/usr/share/Beepsky # Published files will go here
%{__mkdir_p} %{buildroot}/etc/systemd/system # Systemd service file will go here

# Install all published files to /usr/share/Beepsky/
cp -r Beepsky/bin/Release/net10.0/linux-x64/publish/* %{buildroot}/usr/share/Beepsky/

# Create symlink in /usr/local/bin/Beepsky pointing to the actual binary
ln -s ../../share/Beepsky/Beepsky %{buildroot}/usr/local/bin/Beepsky

# Install the systemd service file
%{__install} -m0644 Beepsky/beepsky.service %{buildroot}/etc/systemd/system/beepsky.service

%files
/usr/local/bin/Beepsky
/usr/share/Beepsky/
/etc/systemd/system/beepsky.service

%post
systemctl daemon-reload
systemctl enable --now beepsky.service

%preun
systemctl disable --now beepsky.service

%changelog
* Fri Dec 19 2025 Nickolas Gupton <nickolas@gupton.xyz> - 0.0.1-1
- Initial RPM release of Beepsky Discord Bot