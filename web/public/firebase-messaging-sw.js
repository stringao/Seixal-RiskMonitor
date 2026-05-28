// Firebase Messaging Service Worker for GeoRisk
// This file should be placed in the public folder as firebase-messaging-sw.js

import { initializeApp } from 'firebase/app';
import { getMessaging, onBackgroundMessage } from 'firebase/messaging';

// Firebase configuration - replace with your actual config
const firebaseConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY || '',
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN || '',
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID || '',
  storageBucket: process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET || '',
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID || '',
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID || ''
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);

// Handle background messages
onBackgroundMessage(messaging, (payload) => {
  console.log('Received background message:', payload);

  const notificationTitle = payload.notification?.title || 'GeoRisk Alert';
  const notificationOptions = {
    body: payload.notification?.body || payload.data?.message || 'New alert notification',
    icon: payload.data?.icon || '/bell-icon.png',
    badge: payload.data?.badge || '/badge-icon.png',
    tag: payload.data?.tag || 'georisk-alert',
    data: {
      alertId: payload.data?.alertId,
      severity: payload.data?.severity,
      latitude: payload.data?.latitude,
      longitude: payload.data?.longitude,
      url: payload.data?.url || '/alerts'
    }
  };

  // Show browser notification
  self.registration.showNotification(notificationTitle, notificationOptions);
});

// Handle notification click
self.addEventListener('notificationclick', (event) => {
  console.log('Notification click:', event);
  
  event.notification.close();

  const urlToOpen = event.notification.data?.url || '/alerts';

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then((windowClients) => {
      // Focus existing window if available
      for (const client of windowClients) {
        if (client.url.includes('localhost') && 'focus' in client) {
          return client.focus();
        }
      }
      // Open new window
      if (clients.openWindow) {
        return clients.openWindow(urlToOpen);
      }
    })
  );
});