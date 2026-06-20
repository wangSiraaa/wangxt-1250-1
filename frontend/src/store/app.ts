import { defineStore } from 'pinia'
import { ref, shallowRef } from 'vue'
import api from '@/api'
import type {
  DashboardStats,
  AlarmTrendData,
  TaskStatusDistribution,
  CraneWorkloadData,
  AlarmTypeDistribution,
  Person,
  TowerCrane,
  LiftingTask,
  Alarm,
  Rectification
} from '@/api'

export const useAppStore = defineStore('app', {
  state: () => ({
    stats: shallowRef<DashboardStats | null>(null),
    alarmTrend: shallowRef<AlarmTrendData[]>([]),
    taskStatusDistribution: shallowRef<TaskStatusDistribution[]>([]),
    craneWorkload: shallowRef<CraneWorkloadData[]>([]),
    alarmTypeDistribution: shallowRef<AlarmTypeDistribution[]>([]),
    persons: ref<Person[]>([]),
    towerCranes: ref<TowerCrane[]>([]),
    liftingTasks: ref<LiftingTask[]>([]),
    alarms: ref<Alarm[]>([]),
    rectifications: ref<Rectification[]>([])
  }),
  actions: {
    async fetchDashboardStats() {
      const { data } = await api.dashboard.getStats()
      this.stats = data
      return data
    },

    async fetchAlarmTrend(days: number = 7) {
      const { data } = await api.dashboard.getAlarmTrend(days)
      this.alarmTrend = data
      return data
    },

    async fetchTaskStatusDistribution() {
      const { data } = await api.dashboard.getTaskStatusDistribution()
      this.taskStatusDistribution = data
      return data
    },

    async fetchCraneWorkload() {
      const { data } = await api.dashboard.getCraneWorkload()
      this.craneWorkload = data
      return data
    },

    async fetchAlarmTypeDistribution() {
      const { data } = await api.dashboard.getAlarmTypeDistribution()
      this.alarmTypeDistribution = data
      return data
    },

    async fetchPersons() {
      const { data } = await api.persons.getAll()
      this.persons = data
      return data
    },

    async fetchTowerCranes() {
      const { data } = await api.towerCranes.getAll()
      this.towerCranes = data
      return data
    },

    async fetchLiftingTasks() {
      const { data } = await api.liftingTasks.getAll()
      this.liftingTasks = data
      return data
    },

    async fetchAlarms() {
      const { data } = await api.alarms.getAll()
      this.alarms = data
      return data
    },

    async fetchRectifications() {
      const { data } = await api.rectifications.getAll()
      this.rectifications = data
      return data
    }
  }
})
