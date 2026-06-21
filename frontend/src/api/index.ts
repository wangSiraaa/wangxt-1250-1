import request from './request'
import type { AxiosResponse } from 'axios'

export interface Person {
  id: number
  name: string
  employeeNo: string
  idCard: string
  phone: string
  role: number
  driverStatus?: number | null
  hireDate?: string | null
  isActive: boolean
  createdAt: string
  updatedAt?: string | null
  certificates?: Certificate[]
}

export interface Certificate {
  id: number
  personId: number
  certificateType: number
  certificateNo: string
  issuingAuthority: string
  issueDate: string
  expiryDate: string
  isExpired?: boolean
  daysUntilExpiry?: number
  filePath?: string | null
  isActive: boolean
  createdAt: string
  updatedAt?: string | null
}

export interface TowerCrane {
  id: number
  craneNo: string
  model: string
  serialNo: string
  location: string
  status: number
  ratedLoadCapacity: number
  maxRadius: number
  maxHeight: number
  installDate?: string | null
  lastInspectionDate?: string | null
  nextInspectionDate?: string | null
  blackBoxDeviceId?: string | null
  isActive: boolean
  createdAt: string
  updatedAt?: string | null
  tasks?: LiftingTask[]
  alarms?: Alarm[]
  rectifications?: Rectification[]
}

export interface LiftingTask {
  id: number
  taskNo: string
  towerCraneId: number
  safetyOfficerId?: number | null
  driverId?: number | null
  description: string
  location: string
  plannedLoad: number
  loadType: string
  radius: number
  liftHeight: number
  riskLevel: number
  status: number
  plannedStartTime: string
  plannedEndTime: string
  actualStartTime?: string | null
  actualEndTime?: string | null
  driverConfirmTime?: string | null
  isLowRiskOnly: boolean
  remarks?: string | null
  createdAt: string
  updatedAt?: string | null
  towerCrane?: TowerCrane
  safetyOfficer?: Person
  driver?: Person
  alarms?: Alarm[]
}

export interface Alarm {
  id: number
  towerCraneId: number
  liftingTaskId?: number | null
  alarmType: number
  alarmLevel: number
  status: number
  description: string
  loadValue?: number | null
  loadPercentage?: number | null
  heightValue?: number | null
  radiusValue?: number | null
  rotationValue?: number | null
  windSpeed?: number | null
  alarmTime: string
  processStartTime?: string | null
  resolvedTime?: string | null
  handledById?: number | null
  handleAction?: string | null
  handleRemarks?: string | null
  requiresRectification: boolean
  expectedRectificationTime?: string | null
  blocksLiftingOperation?: boolean
  towerCrane?: TowerCrane
  liftingTask?: LiftingTask
  handledBy?: Person
}

export interface Rectification {
  id: number
  rectificationNo: string
  towerCraneId: number
  sourceAlarmId?: number | null
  alarmId?: number | null
  createdById: number
  reviewedById?: number | null
  assignedToId?: number | null
  title?: string | null
  description: string
  rectificationCategory?: string | null
  actionRequired?: string | null
  actionsTaken?: string | null
  results?: string | null
  reviewComments?: string | null
  priority: number
  status: number
  createdAt: string
  dueDate: string
  deadline?: string
  startTime?: string | null
  executedTime?: string | null
  submittedTime?: string | null
  reviewedTime?: string | null
  closedTime?: string | null
  reviewTime?: string | null
  rectificationActions?: string | null
  reviewResult?: string | null
  remarks?: string | null
  restrictsHighRiskTasks?: boolean
  isOverdue?: boolean
  towerCrane?: TowerCrane
  sourceAlarm?: Alarm
  alarm?: Alarm
  createdBy?: Person
  reviewedBy?: Person
  assignedTo?: Person
}

export interface DashboardStats {
  totalCranes: number
  workingCranes: number
  warningCranes: number
  maintenanceCranes: number
  totalDrivers: number
  qualifiedDrivers: number
  driversWithExpiringCertificates: number
  totalTasksToday: number
  inProgressTasks: number
  completedTasksToday: number
  pendingAlarms: number
  criticalAlarms: number
  openRectifications: number
  urgentRectifications: number
}

export interface AlarmTrendData {
  date: string
  totalAlarms: number
  criticalAlarms: number
  warningAlarms: number
  resolvedAlarms: number
}

export interface TaskStatusDistribution {
  status: number
  count: number
}

export interface CraneWorkloadData {
  craneId: number
  craneNo: string
  completedTasks: number
  inProgressTasks: number
  totalAlarms: number
}

export interface AlarmTypeDistribution {
  type: number
  count: number
  highestLevel: number
}

const api = {
  dashboard: {
    getStats: (): Promise<AxiosResponse<DashboardStats>> =>
      request.get('/dashboard/stats'),
    getAlarmTrend: (days: number = 7): Promise<AxiosResponse<AlarmTrendData[]>> =>
      request.get(`/dashboard/alarm-trend?days=${days}`),
    getTaskStatusDistribution: (): Promise<AxiosResponse<TaskStatusDistribution[]>> =>
      request.get('/dashboard/task-status-distribution'),
    getCraneWorkload: (): Promise<AxiosResponse<CraneWorkloadData[]>> =>
      request.get('/dashboard/crane-workload'),
    getAlarmTypeDistribution: (): Promise<AxiosResponse<AlarmTypeDistribution[]>> =>
      request.get('/dashboard/alarm-type-distribution')
  },

  persons: {
    getAll: (): Promise<AxiosResponse<Person[]>> =>
      request.get('/persons'),
    getById: (id: number): Promise<AxiosResponse<Person>> =>
      request.get(`/persons/${id}`),
    getDrivers: (): Promise<AxiosResponse<Person[]>> =>
      request.get('/persons/drivers'),
    getSafetyOfficers: (): Promise<AxiosResponse<Person[]>> =>
      request.get('/persons/safety-officers'),
    getSupervisors: (): Promise<AxiosResponse<Person[]>> =>
      request.get('/persons/supervisors'),
    checkQualification: (id: number): Promise<AxiosResponse<boolean>> =>
      request.get(`/persons/${id}/qualification-check`),
    getQualificationIssues: (id: number): Promise<AxiosResponse<string[]>> =>
      request.get(`/persons/${id}/qualification-issues`),
    create: (data: Partial<Person>): Promise<AxiosResponse<Person>> =>
      request.post('/persons', data),
    update: (id: number, data: Partial<Person>): Promise<AxiosResponse<Person>> =>
      request.put(`/persons/${id}`, data),
    delete: (id: number): Promise<AxiosResponse<void>> =>
      request.delete(`/persons/${id}`)
  },

  towerCranes: {
    getAll: (): Promise<AxiosResponse<TowerCrane[]>> =>
      request.get('/tower-cranes'),
    getById: (id: number): Promise<AxiosResponse<TowerCrane>> =>
      request.get(`/tower-cranes/${id}`),
    canExecuteRiskLevelTask: (id: number, riskLevel: number): Promise<AxiosResponse<boolean>> =>
      request.get(`/tower-cranes/${id}/can-execute/${riskLevel}`),
    hasPendingCriticalAlarms: (id: number): Promise<AxiosResponse<boolean>> =>
      request.get(`/tower-cranes/${id}/has-pending-critical-alarms`),
    hasOpenRectification: (id: number): Promise<AxiosResponse<boolean>> =>
      request.get(`/tower-cranes/${id}/has-open-rectification`),
    create: (data: Partial<TowerCrane>): Promise<AxiosResponse<TowerCrane>> =>
      request.post('/tower-cranes', data),
    update: (id: number, data: Partial<TowerCrane>): Promise<AxiosResponse<TowerCrane>> =>
      request.put(`/tower-cranes/${id}`, data),
    updateStatus: (id: number, status: number): Promise<AxiosResponse<void>> =>
      request.put(`/tower-cranes/${id}/status`, status),
    delete: (id: number): Promise<AxiosResponse<void>> =>
      request.delete(`/tower-cranes/${id}`)
  },

  liftingTasks: {
    getAll: (): Promise<AxiosResponse<LiftingTask[]>> =>
      request.get('/lifting-tasks'),
    getById: (id: number): Promise<AxiosResponse<LiftingTask>> =>
      request.get(`/lifting-tasks/${id}`),
    getByTowerCraneId: (towerCraneId: number): Promise<AxiosResponse<LiftingTask[]>> =>
      request.get(`/lifting-tasks/tower-crane/${towerCraneId}`),
    getByDriverId: (driverId: number): Promise<AxiosResponse<LiftingTask[]>> =>
      request.get(`/lifting-tasks/driver/${driverId}`),
    getByStatus: (status: number): Promise<AxiosResponse<LiftingTask[]>> =>
      request.get(`/lifting-tasks/status/${status}`),
    validateStart: (id: number): Promise<AxiosResponse<string[]>> =>
      request.get(`/lifting-tasks/${id}/validate-start`),
    validateCreation: (data: Partial<LiftingTask>): Promise<AxiosResponse<string[]>> =>
      request.post('/lifting-tasks/validate-creation', data),
    create: (data: Partial<LiftingTask>): Promise<AxiosResponse<LiftingTask>> =>
      request.post('/lifting-tasks', data),
    update: (id: number, data: Partial<LiftingTask>): Promise<AxiosResponse<LiftingTask>> =>
      request.put(`/lifting-tasks/${id}`, data),
    delete: (id: number): Promise<AxiosResponse<void>> =>
      request.delete(`/lifting-tasks/${id}`),
    submit: (id: number): Promise<AxiosResponse<LiftingTask>> =>
      request.post(`/lifting-tasks/${id}/submit`),
    driverConfirm: (id: number, driverId: number): Promise<AxiosResponse<LiftingTask>> =>
      request.post(`/lifting-tasks/${id}/driver-confirm`, { driverId }),
    start: (id: number): Promise<AxiosResponse<LiftingTask>> =>
      request.post(`/lifting-tasks/${id}/start`),
    complete: (id: number): Promise<AxiosResponse<LiftingTask>> =>
      request.post(`/lifting-tasks/${id}/complete`),
    cancel: (id: number, reason: string): Promise<AxiosResponse<LiftingTask>> =>
      request.post(`/lifting-tasks/${id}/cancel`, { reason })
  },

  alarms: {
    getAll: (): Promise<AxiosResponse<Alarm[]>> =>
      request.get('/alarms'),
    getById: (id: number): Promise<AxiosResponse<Alarm>> =>
      request.get(`/alarms/${id}`),
    getByTowerCraneId: (towerCraneId: number): Promise<AxiosResponse<Alarm[]>> =>
      request.get(`/alarms/tower-crane/${towerCraneId}`),
    getByStatus: (status: number): Promise<AxiosResponse<Alarm[]>> =>
      request.get(`/alarms/status/${status}`),
    getPending: (): Promise<AxiosResponse<Alarm[]>> =>
      request.get('/alarms/pending'),
    getBlockingForTask: (taskId: number): Promise<AxiosResponse<Alarm[]>> =>
      request.get(`/alarms/blocking/task/${taskId}`),
    getBlockingForTowerCrane: (towerCraneId: number): Promise<AxiosResponse<Alarm[]>> =>
      request.get(`/alarms/blocking/tower-crane/${towerCraneId}`),
    create: (data: Partial<Alarm>): Promise<AxiosResponse<Alarm>> =>
      request.post('/alarms', data),
    startProcessing: (id: number, handledById: number): Promise<AxiosResponse<Alarm>> =>
      request.post(`/alarms/${id}/start-processing`, { handledById }),
    resolve: (id: number, action: string, remarks: string, requiresRectification: boolean, expectedRectificationTime?: string | null): Promise<AxiosResponse<Alarm>> =>
      request.post(`/alarms/${id}/resolve`, { action, remarks, requiresRectification, expectedRectificationTime }),
    ignore: (id: number, handledById: number, reason: string): Promise<AxiosResponse<Alarm>> =>
      request.post(`/alarms/${id}/ignore`, { handledById, reason })
  },

  rectifications: {
    getAll: (): Promise<AxiosResponse<Rectification[]>> =>
      request.get('/rectifications'),
    getById: (id: number): Promise<AxiosResponse<Rectification>> =>
      request.get(`/rectifications/${id}`),
    getByTowerCraneId: (towerCraneId: number): Promise<AxiosResponse<Rectification[]>> =>
      request.get(`/rectifications/tower-crane/${towerCraneId}`),
    getByStatus: (status: number): Promise<AxiosResponse<Rectification[]>> =>
      request.get(`/rectifications/status/${status}`),
    getOpen: (): Promise<AxiosResponse<Rectification[]>> =>
      request.get('/rectifications/open'),
    create: (data: Partial<Rectification>): Promise<AxiosResponse<Rectification>> =>
      request.post('/rectifications', data),
    update: (id: number, data: Partial<Rectification>): Promise<AxiosResponse<Rectification>> =>
      request.put(`/rectifications/${id}`, data),
    delete: (id: number): Promise<AxiosResponse<void>> =>
      request.delete(`/rectifications/${id}`),
    assign: (id: number, assignedToId: number): Promise<AxiosResponse<Rectification>> =>
      request.post(`/rectifications/${id}/assign`, { assignedToId }),
    start: (id: number): Promise<AxiosResponse<Rectification>> =>
      request.post(`/rectifications/${id}/start`),
    execute: (id: number, actionsTaken: string, results: string): Promise<AxiosResponse<Rectification>> =>
      request.post(`/rectifications/${id}/execute`, { actionsTaken, results }),
    submitForReview: (id: number, actions: string): Promise<AxiosResponse<Rectification>> =>
      request.post(`/rectifications/${id}/submit-review`, { actions }),
    review: (id: number, reviewerId: number, approved: boolean, comments: string): Promise<AxiosResponse<Rectification>> =>
      request.post(`/rectifications/${id}/review`, { reviewerId, approved, comments, reviewResult: comments }),
    close: (id: number): Promise<AxiosResponse<Rectification>> =>
      request.post(`/rectifications/${id}/close`),
    reject: (id: number, reviewerId: number, reviewResult: string): Promise<AxiosResponse<Rectification>> =>
      request.post(`/rectifications/${id}/reject`, { reviewerId, reviewResult })
  }
}

export default api
