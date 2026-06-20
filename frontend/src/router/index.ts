import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/dashboard'
  },
  {
    path: '/dashboard',
    name: 'Dashboard',
    component: () => import('@/views/Dashboard.vue'),
    meta: { title: '监控中心' }
  },
  {
    path: '/persons',
    name: 'Persons',
    component: () => import('@/views/Persons.vue'),
    meta: { title: '人员管理' }
  },
  {
    path: '/tower-cranes',
    name: 'TowerCranes',
    component: () => import('@/views/TowerCranes.vue'),
    meta: { title: '塔吊管理' }
  },
  {
    path: '/lifting-tasks',
    name: 'LiftingTasks',
    component: () => import('@/views/LiftingTasks.vue'),
    meta: { title: '吊装任务管理' }
  },
  {
    path: '/alarms',
    name: 'Alarms',
    component: () => import('@/views/Alarms.vue'),
    meta: { title: '报警管理' }
  },
  {
    path: '/rectifications',
    name: 'Rectifications',
    component: () => import('@/views/Rectifications.vue'),
    meta: { title: '整改管理' }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.afterEach((to) => {
  const title = (to.meta?.title as string) || '塔吊安全监控系统'
  document.title = title + ' - 塔吊安全监控系统'
})

export default router
