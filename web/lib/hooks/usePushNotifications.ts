'use client';

import { useEffect, useState, useCallback } from 'react';

interface PushSubscriptionData {
  endpoint: string;
  p256dh: string;
  auth: string;
}

interface NotificationPreferences {
  pushSubscriptions: Array<{
    id: string;
    deviceType: string;
    isActive: boolean;
    lastNotifiedAt: string | null;
  }>;
  citizenZones: Array<{
    id: string;
    radiusKm: number;
    eventTypes: string[];
    severityThreshold: string;
  }>;
}

interface ZoneSubscription {
  zoneId: string;
  verificationToken?: string;
}

const VAPID_PUBLIC_KEY = process.env.NEXT_PUBLIC_VAPID_PUBLIC_KEY || '';

// Convert base64 to Uint8Array for VAPID key
function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}

export function usePushNotifications() {
  const [isSupported, setIsSupported] = useState(false);
  const [isSubscribed, setIsSubscribed] = useState(false);
  const [subscription, setSubscription] = useState<PushSubscriptionData | null>(null);
  const [permission, setPermission] = useState<NotificationPermission>('default');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Check if push notifications are supported
  useEffect(() => {
    if (typeof window !== 'undefined' && 'serviceWorker' in navigator && 'PushManager' in window) {
      setIsSupported(true);
      setPermission(Notification.permission);
      
      // Check existing subscription
      checkExistingSubscription();
    }
  }, []);

  const checkExistingSubscription = async () => {
    try {
      const registration = await navigator.serviceWorker.ready;
      const existingSub = await registration.pushManager.getSubscription();
      
      if (existingSub) {
        const data = extractSubscriptionData(existingSub);
        setSubscription(data);
        setIsSubscribed(true);
      }
    } catch (err) {
      console.error('Error checking existing subscription:', err);
    }
  };

  const extractSubscriptionData = (sub: PushSubscription): PushSubscriptionData => {
    const p256dh = sub.toJSON().keys?.p256dh || '';
    const auth = sub.toJSON().keys?.auth || '';
    const endpoint = sub.endpoint || '';
    
    return { endpoint, p256dh, auth };
  };

  const subscribe = useCallback(async (deviceType: string = 'browser'): Promise<boolean> => {
    if (!isSupported) {
      setError('Push notifications are not supported in this browser');
      return false;
    }

    setLoading(true);
    setError(null);

    try {
      const registration = await navigator.serviceWorker.ready;
      
      // Request notification permission
      const perm = await Notification.requestPermission();
      if (perm !== 'granted') {
        setError('Notification permission denied');
        setLoading(false);
        return false;
      }

      setPermission(perm);

      // Subscribe to push
      const sub = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(VAPID_PUBLIC_KEY) as BufferSource
      });

      const data = extractSubscriptionData(sub);
      setSubscription(data);
      setIsSubscribed(true);

      // Send subscription to server
      const response = await fetch('/api/notifications/subscribe', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          endpoint: data.endpoint,
          p256dh: data.p256dh,
          auth: data.auth,
          deviceType
        })
      });

      if (!response.ok) {
        throw new Error('Failed to register subscription with server');
      }

      setLoading(false);
      return true;
    } catch (err) {
      console.error('Error subscribing to push:', err);
      setError(err instanceof Error ? err.message : 'Failed to subscribe');
      setLoading(false);
      return false;
    }
  }, [isSupported]);

  const unsubscribe = useCallback(async (): Promise<boolean> => {
    if (!subscription) return false;

    setLoading(true);
    setError(null);

    try {
      const registration = await navigator.serviceWorker.ready;
      const existingSub = await registration.pushManager.getSubscription();
      
      if (existingSub) {
        await existingSub.unsubscribe();
      }

      // Notify server
      await fetch('/api/notifications/unsubscribe', {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ endpoint: subscription.endpoint })
      });

      setSubscription(null);
      setIsSubscribed(false);
      setLoading(false);
      return true;
    } catch (err) {
      console.error('Error unsubscribing:', err);
      setError(err instanceof Error ? err.message : 'Failed to unsubscribe');
      setLoading(false);
      return false;
    }
  }, [subscription]);

  const updateSubscription = useCallback(async (
    newEndpoint: string,
    newP256dh: string,
    newAuth: string
  ): Promise<boolean> => {
    if (!subscription) return false;

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/notifications/update', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          oldEndpoint: subscription.endpoint,
          newEndpoint,
          p256dh: newP256dh,
          auth: newAuth
        })
      });

      if (!response.ok) {
        throw new Error('Failed to update subscription');
      }

      setSubscription({ endpoint: newEndpoint, p256dh: newP256dh, auth: newAuth });
      setLoading(false);
      return true;
    } catch (err) {
      console.error('Error updating subscription:', err);
      setError(err instanceof Error ? err.message : 'Failed to update subscription');
      setLoading(false);
      return false;
    }
  }, [subscription]);

  return {
    isSupported,
    isSubscribed,
    subscription,
    permission,
    loading,
    error,
    subscribe,
    unsubscribe,
    updateSubscription
  };
}

// Zone subscription hook
export function useZoneAlerts() {
  const [zones, setZones] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadZones = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/notifications/zones');
      if (!response.ok) throw new Error('Failed to load zones');
      
      const data = await response.json();
      setZones(data.zones || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load zones');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadZones();
  }, [loadZones]);

  const createZone = async (
    latitude: number,
    longitude: number,
    radiusKm: number = 10,
    eventTypes: string[] = ['Fire', 'Flood', 'Storm'],
    severityThreshold: string = 'Medium',
    phone?: string
  ): Promise<ZoneSubscription | null> => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/notifications/zones', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          latitude,
          longitude,
          radiusKm,
          eventTypes,
          severityThreshold,
          phone
        })
      });

      if (!response.ok) throw new Error('Failed to create zone');
      
      const data = await response.json();
      await loadZones();
      return data;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create zone');
      return null;
    } finally {
      setLoading(false);
    }
  };

  const deleteZone = async (zoneId: string): Promise<boolean> => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`/api/notifications/zones/${zoneId}`, {
        method: 'DELETE'
      });

      if (!response.ok) throw new Error('Failed to delete zone');
      
      await loadZones();
      return true;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete zone');
      return false;
    } finally {
      setLoading(false);
    }
  };

  const verifyZone = async (zoneId: string, token: string): Promise<boolean> => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`/api/notifications/zones/${zoneId}/verify`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token })
      });

      if (!response.ok) throw new Error('Failed to verify zone');
      
      await loadZones();
      return true;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to verify zone');
      return false;
    } finally {
      setLoading(false);
    }
  };

  return {
    zones,
    loading,
    error,
    createZone,
    deleteZone,
    verifyZone,
    refresh: loadZones
  };
}

// Preferences hook
export function useNotificationPreferences() {
  const [preferences, setPreferences] = useState<NotificationPreferences | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPreferences = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/notifications/preferences');
      if (!response.ok) throw new Error('Failed to load preferences');
      
      const data = await response.json();
      setPreferences(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load preferences');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadPreferences();
  }, [loadPreferences]);

  const updatePreferences = async (severityThreshold?: string): Promise<boolean> => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/notifications/preferences', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ severityThreshold })
      });

      if (!response.ok) throw new Error('Failed to update preferences');
      
      await loadPreferences();
      return true;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update preferences');
      return false;
    } finally {
      setLoading(false);
    }
  };

  return {
    preferences,
    loading,
    error,
    updatePreferences,
    refresh: loadPreferences
  };
}