# Arayuz Liste Standardi

Bu projede yeni bir veri listesi eklendiginde asagidaki davranislar zorunludur:

- Liste icinde canli arama, sayfa boyutu secimi, sayfalama ve sayfa atlama bulunur.
- Siralanabilir alanlar kolon basligindaki dugmeyle artan/azalan siralanir.
- Metin siralamasi Turkce duyarlidir; sayisal alanlar sayisal siralanir.
- Filtre, sayfa boyutu veya siralama degistiginde liste ilk sayfaya doner.
- Bos sonuc durumu gorunur olur; URL query parametresiyle liste durumu yazilmaz.
- Her satirin islem alaninda bir `Detay` dugmesi bulunur. Duzenleme, pasiflestirme ve silme aksiyonlari bu acilimda yer alir.
- Filtre veya sayfa degistiginde gorunmeyen satirin acik detayi kapatilir.

Mevcut istemci altyapisi `BankUrun.Web/wwwroot/js/site.js` icindeki `setupList` fonksiyonudur. Yeni liste ekranlari bu yapinin `data-list`, `data-list-filter`, `data-list-sort` ve pagination data attribute sozlesmesini kullanmalidir.
