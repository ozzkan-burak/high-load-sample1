import http from 'k6/http';
import { check, sleep } from 'k6';

// Mimarın Test Stratejisi
export const options = {
  stages: [
    { duration: '10s', target: 100 }, // Isınma: 10 saniyede 100 kullanıcıya çık
    { duration: '30s', target: 1000 }, // Yüklenme: 30 saniye boyunca 1000 kullanıcıyla saldır
    { duration: '10s', target: 0 }, // Soğuma: Yavaşça dur
  ],
};

export default function () {
  // 1. API'ye İstek At (GET /api/ticket)
  const res = http.get('http://localhost:5000/api/ticket');

  // 2. Cevabı Kontrol Et (200 OK döndü mü?)
  check(res, {
    'status is 200': (r) => r.status === 200,
    'cevap suresi < 500ms': (r) => r.timings.duration < 500, // Redis varsa 500ms altı olmalı
  });

  // Her kullanıcı istek attıktan sonra 0.1 saniye beklesin (Gerçekçi olsun diye)
  sleep(0.1);
}
