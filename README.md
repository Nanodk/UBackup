# UBackup

UBackup is a powerfull and fast tool to backup and restore your data on the UseNet network.

It was developed to answer to various problematics:

* Backup a huge quantity of data
* Restore easily your data
* Secure your data to prevent steal
* Low cost online backup storage with high data retention (cloud backup storage alternative)
* Easy use with minimal settings

The name UBackup is a mix between Usenet and Backup.

## Requirements

UBackup is fully compatible with Windows computers and servers. It required only DotNet Framework 4.7 to work.

A compatibility with others platforms (like Linux) seems to be possible because all the code could be compiled under DotNet Core framework.

## Features

* Backup multiple folders to UseNet
* Fast upload and multiconnection to usenet
* Multiple group posting
* Check and repost missing or failed article post
* Integrated file splitter
* Data encryption for better security
* Generated PAR2 parity files for each file backuped
* Automatically repost after a retention day
* Independant NZB for each file
* Database auto save and zip
* Checksum of each file
* Tree export for fast search and overview
* Console mode application
* Low resource consumption (CPU and memory)
* Easy to use

## Installation

Download UBackup in Release section and unzip the content into a folder of your choice (like C:\UBackup).

Edit the file settings.conf with your favorite text editor and fill the parameters.

Run UBackup.exe and it's all !

## Parameters

You need to overwrite some settings in file settings.conf

### General Settings

* ENCRYPTION_KEY: a passphrase of your choice to protect your uploads
* IGNORE_EXT: files to ignore for backup (comma separated)
* IGNORE_FILESIZE_MIN: minimum filesize for files to backup

### NewsGroup Settings

* NEWSGROUP_SERVER: newsgroup server to use
* NEWSGROUP_PORT: SSL port of your server
* NEWSGROUP_USER: your newsgroup username
* NEWSGROUP_PASSWORD: your newsgroup password
* NEWSGROUP_RETENTION: number of days to wait before reupload a file (have to be less than the newsgroup server retention)
* NEWSGROUP_CONNECTIONS: max conns to use for upload and download

### Upload Settings

* UPLOAD_ARTICLE_SIZE: defines the size (in K) for each article posted (recommanded 700)
* UPLOAD_FILE_SUFFIX: suffix to add to each uploaded file
* UPLOAD_GROUPS: group to post your files (comma separated, ex: alt.binaries.backup, alt.boneless)
* UPLOAD_POSTER: defines poster name and email
* UPLOAD_POSTER_RANDOM: 0 or 1 for generate random poster name and email for each file. If set to 1, UPLOAD_POSTER parameter is ignore.

### Folders Settings

* FOLDER: defines folder you want to backup. For backup multiple directories, you can set multiples FOLDER= lines in settings.conf

## How UBackup works

UBackup is a software which run in console (known as terminal or background mode too) or in batch (background) command line.

UBackup (console or command line) purposes 4 differents modes

### Backup mode

Backup mode allows you to backup your data specified in the settings.conf file. First each file to backup is encrypted and splitted. Next, parity files are generated. Finally, chunk files and parity files are posted to your newsgroup server.

If an upload is successful, a specific NZB file is stored to Nzb folder and the database is updated.

When UBackup is used in backup mode it adds only new file. It never remove entries from is database. So if you delete FOLDER= entries in your settings.conf file, previous files added aren't removed.

### Restore mode

Restore mode allows you to recovery your data. You only need to specify which directory you want to recover and UBackup downloads your files from your newsgroup server. It automatically join, decrypt and check downloaded files.

If a download is successful, recovery file is moved into Restore folder.

### Zip mode

This mode permits to backup the entire database folder (Nzb folder) into a zip file stored in Zip folder.

### Tree mode

Tree outputs a file named tree.txt which contains a treeview of all directories and files stored in database.

## Command line use

For some reasons it could be interesting to use UBackup in command line, per example to program multiple restoration using a batch file.

Available command line args:

```dos
-b: Backup
-r "folder_path": Restore
-z: Zip nzb folder
-t: Tree database
```

## Screenshot

[[https://github.com/jhdscript/UBackup/blob/master/ubackup.png|alt=ubackup]]

As you can see in this screenshot, UBackup is very performant and it could be used to backup or restore large volume of data. Statistics are displayed in the window title.

## Technical information

UBackup is develop in VB.Net because .Net Framework is fast and present in all Windows platform.

### Logical Tree

The logical tree explains how is structured UBackup. [D] is for directory and [F] for file.

```
[D] UBackup Folder
---[F] UBackup.exe: main program
---[F] UBackup.pdb: debug file
---[F] UBackup.log: output log file
---[F] settings.conf: UBackup configuration file
---[F] Newtonsoft.Json.dll: JSON library
---[D] Bin: contains third parties
------[F] nyuu.exe: for posting file
------[F] nzbget.conf: nzb template configuration
------[F] nzbget.exe: for downloading
------[F] par2.exe: for parity files
---[D] Nzb: UBackup database and NZB files
------[D] a-z0-9: folders to store NZB files
------[F] database.db: JSON database file
------[F] tree.txt: database folders and files tree
---[D] Restore: destination folder for Restore mode
---[D] Temp: temporary folder for processing
---[D] Zip: contains database archives
```

### Processing

UBackup operation is easy and the code is optimised and designed for performance. Each mode runs only 2 processing threads.

In Backup mode, a first thread is assigned to Split / Encrypt / Par files. This thread sleeps when 5 files are waiting for upload.

A second thread is used for posting data to usenet. If posting is successful, database is updated, else file is skipped and it will be processed when you restart UBackup.

In Restore mode, a first thread is used for download files from your newsgroup server whereas the second thread is used to Join / Decrypt / Check. The thread which downloads files sleeps when 5 files are waiting for join.

## Planned features

UBackup is under development and some new features will appear in future release

* one file download from nzb file providing encryption key (to allow share with friend :-))
* multi server support

## Reviews

For more information or help you could read online reviews

Coming Soon

## Dependencies

UBackup uses some externals tools or libraries.

* Newtonsoft.Json (MIT License - Copyright (c) 2007 James Newton-King)
* NYUU (Public Domain - Copyright (c) animetosho)
* Par2CmdLine (GNU General Public License v2.0 - Copyright (c) 2017 Ike Devolder)
* NzbGet (GNU General Public License v2.0)

## Known issues

If your usenet provider use HW Media backbone, you could have some issues after posting a large volume of data. This provider is very strange and after a couple of hour of posting it skip a lot of posts.

I recommand you to use a newsgroup provider in another backbone for posting (UseNet tree).

## Changelog

### Version 1.0.0 (first release) - 20190224

* Backup mode is fully working
* Files are now automatically splited into chuncks
* Speeds improvement for encryption and split
* Export database with NZB files in zip archive when program starts
* Fast checksum calculation
* Display stats in executable window title
* I/O Optimisation
* CPU consumption reduced

### Version 0.0.0 (pre version) - 20190120

* First Batch of UBackup
* All program architecture is defined
* All third parties softwares are elected
* Development language is set to VB.Net

## Question / Support / Donation

For any question I recommand you to visit my blog (https://www.zem.fr) or to open a GitHub issue.

UBackup is totally free, but you can help us with a small donation:

* BTC: 1EGeXCdvWz2RdhcPnPuStg8s2TFs18Yi5x
* LTC: LY8uA7JDbg8kQBnLp6DkqEuFD1zNqKayQj
* TRX: TX4xiCK4aS84vmTL9CWK141EFCg53wTtmY
* ETH: 0xf47d7b0c1efa3aa7df0cd5ffb60b6fbd085187eb
* ETC: 0xf47d7b0c1efa3aa7df0cd5ffb60b6fbd085187eb

Paypal donation could be sent to: jhdscript at gmail.com

## License

The MIT License (MIT)

Copyright (c) 2019 jhdscript at gmail.com (www.zem.fr)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.