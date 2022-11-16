# QueueAlert

Her kommer QueueAlert som tracker et tall på skjermen.


**Slik fungerer den**

Man selecter et område først
(Dette fungerer på lik måte som et 'snipping tool')


Denne tar skjermbilde av området, dette gjør den hvert sekund 

Skjermbilde går så igjennom en OCR tjeneste
(Pakken heter Tesseract OCR)

Denne går bildet om til text, som vi kan så parse til int

Deretter er det bare å sjekke om tallet er mindre enn 100
Hva ja, spill av lyd


![](https://sindrema.com/files/Ve0scbIoHS.png)
![image](https://user-images.githubusercontent.com/29127320/202163366-615bbd89-c45b-46b8-ab4f-08e47bac44d4.png)



Er tilgjengelig på Discord SindreMA#9630 skulle du lure på noe