import { createRouter, createWebHashHistory } from 'vue-router'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    {
      path: '/',
      redirect: '/scheme',
    },
    {
      path: '/connect',
      name: 'connect',
      component: () => import('@/views/ConnectView.vue'),
      meta: { title: '连接' },
    },
    {
      path: '/scheme',
      name: 'scheme',
      component: () => import('@/views/SchemeView.vue'),
      meta: { title: '方案' },
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/MobileSettingsView.vue'),
      meta: { title: '设置' },
    },
  ],
})

export default router
