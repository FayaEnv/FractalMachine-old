# FractalMachine
Readme per i collaboratori. Al momento la repository è in fase *embrionale*, ha diversi punti che devono essere districati e da solo non ce la faccio. Per il momento sono in cerca di collaboratori italiani, appena possibile mi preoccuperò di scrivere la documentazione in inglese

## Introduzione 

La *FractalMachine* è un ambiente di sviluppo che sto scrivendo in **C#**. Si basa su un linguaggio che ho chiamato **Light** che sarà simile al C# ma con la differenza che sarà compilato. Per arrivare a ciò la FractalMachine si preoccuperà di leggere il codice Light, convertirlo in **Linear**, una sorta di bytecode, ed infine riscriverlo in **C++** dove sarà compilato dal compilatore di sistema. La FractalMachine sarà un eseguibile unico che permetterà compilazione, debug ma anche funzioni per la scrittura del codice (come un analogo all'IntelliSense)

### Perchè C#

Sì, per un linguaggio compilato un compilatore in **.NET Core** è una strana scelta. Questo per due ragioni: il Light sarà simile al C# e crea maggiore congiunzione con il suo stesso linguaggio di programmazione e infine, data la portata del progetto, uno sviluppo in .NET sarà più agevole e porterebbe ad una implementazione più facile di funzioni che in ambiente C++ richiederebbero più sforzo. Alla fine di tutto il compilatore stesso potrà essere convertito in Light dove potrà essere puramente compilato.

### Perchè C++

Ho scelto di non scrivere un compilatore vero e proprio per garantire la portabilità ed evitare gli enormi costi che richiederebbe sviluppare un compilatore da zero. Infine intendo rendere possibile sfruttare l'enorme mole di codice disponibile in C++ direttamente in Light preferibilmente senza l'ausilio di porting.

## Stato corrente

In questo momento ho scritto il parser Light, il convertitore in Linear e anche il convertitore in C++. **Ma sono rimasto bloccato in un punto delicato**. Inizialmente intendevo far sì che l'ambiente fosse unicamente in ambiente Unix. Perciò, su Windows, al primo avvio il programma si preoccupava di installare una versione ad hoc di **Cygwin** con tanto di gestione repository. Purtroppo la scelta si è rivelata sbagliata: l'uso di Cygwin preclude una buona portabilità degli eseguibili in ambiente Windows. In più, per qualche ragione, non sono riuscito ad ottenere un eseguibile funzionante, che si fermava ad un "permission denied". Forse è il risultato di un'installazione con i permessi sbagliata di Cygwin, il punto è che non è di semplice utilizzo.

### MSVC o MinGW?

Così devo scegliere se supportare più compilatori e non solo GCC come inizialmente pianificato. Però intendo comunque rendere possibile il supporto congiunto alle stesse librerie, cosa che Cygwin o **MSYS2** mi permetterebbero di fare. La compilazione di una libreria su MinGW non [garantisce la sua utilizzabilità su MSVC](http://www.mingw.org/wiki/Interoperability_of_Libraries_Created_by_Different_Compiler_Brands), quindi mi chiedo: **è possibile garantire le stesse librerie sia in ambiente Unix che Windows usando MSVC?**

La scelta finale direi essere questa: creare una repository ad hoc che permetta di ottenere le versioni per ogni sistema operativo ed architettura dello stesso pacchetto. Su Windows, in più, sarà possibile scegliere il compilatore fra MSVC e GCC (o CLang).

### La repository

Per permettere un utilizzo facile dell'ambiente di sviluppo offrirei una repository dove poter scaricare librerie indipendentemente dal sistema operativo scelto. In ambiente Windows sarà possibile scegliere fra la repository MinGW o MSVC, su Linux **Arch** e su Mac OS X **Brew**.

* È possibile sfruttare la repository di Arch Linux per le qualunque distro creando un file system "virtuale" tenendolo separato dal package manager del sistema operativo?

In tal proposito intenderei utilizzare l'env [LD_LIBRARY_PATH](https://unix.stackexchange.com/questions/24811/changing-linked-library-for-a-given-executable-centos-6) per permettere *anche* di includere le librerie precompilate direttamente nella cartella dell'eseguibile. 

## Ho bisogno di aiuto

Per questo progetto ho bisogno di aiuto sia da parte di sviluppatori C# che C++, sia in ambiente Windows che Unix. 

Adesso i punti salienti sono:

- Sviluppare il convertitore Light to C++, legandolo alle esigenze del sistema operativi su cui e per cui viene compilato
- Sviluppare il sistema di gestione dell'ambiente, quindi sistema operativo, architettura, repository e compilatore
- Sviluppare la repository congiunta

Personalmente sono ancora in dubbio fra MSVC e MinGW (o tutte e due).

Se siete interessati contattatemi su rcecchini.ds@gmail.com, a tempo debito creo il progetto su GitHub per la collaborazione congiunta.



Grazie!

Riccardo



## Info aggiuntive

### Struttura delle directory

#### Gerarchie

Se una classe madre (come una astratta) ha classi derivate, queste verranno messe in una cartella con lo stesso nome della classe al plurale. Ma nel caso la classe non avesse un plurale verrà messa direttamente nella cartella avente lo stesso nome. 











