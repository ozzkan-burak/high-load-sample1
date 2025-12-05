import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '10s', target: 50 }, // Isınma
    { duration: '30s', target: 500 }, // 500 Eşzamanlı Kullanıcı (Yazma işlemi için çok yüksektir)
    { duration: '10s', target: 0 }, // Soğuma
  ],
};

export default function () {
  // Rastgele veri üretelim ki DB'de unique görünsün (Basitçe)
  const randomId = Math.floor(Math.random() * 100000);

  const payload = JSON.stringify({
    ownerName: `User_${randomId}`,
    eventName: 'Mega Tarkan Konseri',
    price: 100 + (randomId % 50),
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  // POST isteği atıyoruz
  const res = http.post('http://localhost:5000/api/ticket', payload, params);

  check(res, {
    // DİKKAT: Artık 200 OK veya 201 Created beklemiyoruz.
    // Başarılı kodumuz "202 Accepted" olmalı.
    'status is 202': (r) => r.status === 202,

    // RabbitMQ'ya mesaj bırakmak çok hızlı olmalı (DB yazmayı beklemiyoruz)
    'cevap suresi < 200ms': (r) => r.timings.duration < 200,
  });

  sleep(0.5); // Yarım saniye bekle
}
