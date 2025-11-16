import http from 'k6/http';
import { check, sleep } from 'k6';

// Read the file content during the init stage
const audioFileContent = open('D:\\Fork\\Spred\\spred.api\\microservices\\spred.api.recommendation\\source\\Tests\\IntegratedTest\\audio.mp3', 'b');

export let options = {
  vus: 10,
  duration: '10s',
};

export default function () {
  console.log("Run");

  if (!audioFileContent) {
    console.error('Ошибка: файл не найден или не удалось открыть');
    return;
  }

  // Create form data with the file content
  const formData = {
    audioFile: http.file(audioFileContent, 'audio.mp3', 'audio/mpeg'),
  };

  // Send the POST request
  let res = http.post('http://localhost:5083/inference', formData);

  // Check the response
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 200ms': (r) => r.timings.duration < 200,
  });

  sleep(1);
}