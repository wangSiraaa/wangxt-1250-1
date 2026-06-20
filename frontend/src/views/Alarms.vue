<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h3 style="margin:0">报警管理</h3>
        <span style="color:#909399;font-size:13px">黑匣子报警联动：超载/限位报警将自动阻断吊装作业，需监理处理</span>
      </div>
      <div class="header-actions">
        <el-select v-model="statusFilter" placeholder="处理状态" clearable style="width:140px">
          <el-option v-for="(item, key) in AlarmStatusMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-select v-model="typeFilter" placeholder="报警类型" clearable style="width:140px">
          <el-option v-for="(item, key) in AlarmTypeMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-select v-model="levelFilter" placeholder="报警级别" clearable style="width:120px">
          <el-option v-for="(item, key) in AlarmLevelMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-button type="danger" :icon="WarningFilled" @click="simulateAlarm">模拟报警</el-button>
      </div>
    </div>

    <el-row :gutter="16" style="margin-bottom:16px">
      <el-col :span="6">
        <el-card shadow="hover">
          <el-statistic title="待处理" :value="pendingCount" :value-style="{ color: '#E6A23C' }" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <el-statistic title="严重报警" :value="criticalCount" :value-style="{ color: '#F56C6C' }" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <el-statistic title="处理中" :value="processingCount" :value-style="{ color: '#409EFF' }" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <el-statistic title="今日已解决" :value="todayResolvedCount" :value-style="{ color: '#67C23A' }" />
        </el-card>
      </el-col>
    </el-row>

    <div v-if="blockingAlarms.length" class="linkage-error">
      <div class="error-title">
        <el-icon><WarningFilled /></el-icon>
        阻断性报警（{{ blockingAlarms.length }} 条 - 将自动暂停相关吊装任务）
      </div>
      <div class="error-content">
        联动规则：超载、高度限位、幅度限位等严重报警未处理前，相关塔吊将无法启动新任务，进行中的任务将被自动暂停。
        必须由监理处理并确认解决后方可恢复作业。
      </div>
    </div>

    <el-card shadow="hover">
      <el-table :data="filteredAlarms" stripe v-loading="loading" @row-dblclick="handleAlarm">
        <el-table-column label="编号" prop="id" width="70" align="center" />
        <el-table-column label="报警类型" width="110">
          <template #default="{ row }">
            <el-tag :type="AlarmTypeMap[row.alarmType].type" effect="dark">
              {{ AlarmTypeMap[row.alarmType].label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="级别" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="AlarmLevelMap[row.alarmLevel].type" effect="dark" size="small" round>
              {{ AlarmLevelMap[row.alarmLevel].label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="塔吊" width="100" align="center">
          <template #default="{ row }">{{ row.towerCrane?.craneNo || '—' }}</template>
        </el-table-column>
        <el-table-column label="关联任务" width="150">
          <template #default="{ row }">{{ row.liftingTask?.taskNo || '—' }}</template>
        </el-table-column>
        <el-table-column label="报警详情" min-width="240">
          <template #default="{ row }">
            <div style="font-size:13px">
              <div style="font-weight:500">{{ row.description }}</div>
              <div style="color:#909399;margin-top:4px;display:flex;flex-wrap:wrap;gap:8px">
                <span v-if="row.loadValue">载荷: {{ row.loadValue }}吨 ({{ row.loadPercentage }}%)</span>
                <span v-if="row.radiusValue">半径: {{ row.radiusValue }}m</span>
                <span v-if="row.heightValue">高度: {{ row.heightValue }}m</span>
                <span v-if="row.windSpeed">风速: {{ row.windSpeed }}m/s</span>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="报警时间" width="160">
          <template #default="{ row }">{{ formatDateTime(row.alarmTime) }}</template>
        </el-table-column>
        <el-table-column label="处理人" width="100" align="center">
          <template #default="{ row }">{{ row.handledBy?.name || '—' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="AlarmStatusMap[row.status].type">
              {{ AlarmStatusMap[row.status].label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="联动" width="90" align="center">
          <template #default="{ row }">
            <el-tooltip v-if="row.blocksLiftingOperation" content="阻断吊装作业" placement="top">
              <el-icon :size="18" style="color:#F56C6C"><Lock /></el-icon>
            </el-tooltip>
            <el-tooltip v-else content="提示性报警" placement="top">
              <el-icon :size="18" style="color:#67C23A"><Unlock /></el-icon>
            </el-tooltip>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right" align="center">
          <template #default="{ row }">
            <el-button v-if="row.status === 1" type="primary" link size="small" @click="startProcess(row)">开始处理</el-button>
            <el-button v-if="row.status === 1 || row.status === 2" type="success" link size="small" @click="resolveAlarm(row)">解决</el-button>
            <el-button v-if="row.status === 1 || row.status === 2" type="warning" link size="small" @click="ignoreAlarm(row)">忽略</el-button>
            <el-button type="info" link size="small" @click="viewDetail(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="resolveDialogVisible" title="处理报警" width="600px">
      <div v-if="currentAlarm" style="margin-bottom:16px">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="编号">AL-{{ currentAlarm.id }}</el-descriptions-item>
          <el-descriptions-item label="类型">
            <el-tag :type="AlarmTypeMap[currentAlarm.alarmType].type">
              {{ AlarmTypeMap[currentAlarm.alarmType].label }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="塔吊">{{ currentAlarm.towerCrane?.craneNo }}</el-descriptions-item>
          <el-descriptions-item label="时间">{{ formatDateTime(currentAlarm.alarmTime) }}</el-descriptions-item>
          <el-descriptions-item label="描述" :span="2">{{ currentAlarm.description }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="linkage-warning">
        <div class="warning-title">
          <el-icon><Tips /></el-icon>
          联动影响说明
        </div>
        <div class="warning-content">
          <p>1. 解决后将自动：</p>
          <ul style="padding-left:20px;margin:4px 0">
            <li>更新报警状态为已解决</li>
            <li>尝试恢复因本报警暂停的吊装任务</li>
            <li v-if="requiresRectification">自动生成整改单，整改期间塔吊仅允许低风险任务</li>
            <li v-else>若无其他阻断性报警，塔吊状态恢复正常</li>
          </ul>
        </div>
      </div>

      <el-form :model="resolveForm" label-width="90px" style="margin-top:16px">
        <el-form-item label="处理动作" required>
          <el-input v-model="resolveForm.action" type="textarea" :rows="2" placeholder="如：调整力矩限制器参数、重新标定幅度限位、暂停高风险作业..." />
        </el-form-item>
        <el-form-item label="备注说明">
          <el-input v-model="resolveForm.remarks" type="textarea" :rows="2" placeholder="处理过程说明、复测结果等" />
        </el-form-item>
        <el-form-item label="处理人" required>
          <el-select v-model="resolveForm.handledById" placeholder="选择监理" style="width:100%">
            <el-option v-for="s in supervisors" :key="s.id" :label="s.name" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="是否需整改">
          <el-switch v-model="requiresRectification" />
          <span v-if="requiresRectification" style="color:#E6A23C;margin-left:8px">
            （将自动生成整改单，整改期间塔吊仅允许低风险任务）
          </span>
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="resolveDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="resolving" @click="confirmResolve">确认解决</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="ignoreDialogVisible" title="忽略报警" width="500px">
      <el-form :model="ignoreForm" label-width="90px">
        <el-form-item label="处理人" required>
          <el-select v-model="ignoreForm.handledById" placeholder="选择监理" style="width:100%">
            <el-option v-for="s in supervisors" :key="s.id" :label="s.name" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="忽略原因" required>
          <el-input v-model="ignoreForm.reason" type="textarea" :rows="3" placeholder="请说明忽略此报警的原因，如：测试报警、瞬时波动等" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="ignoreDialogVisible = false">取消</el-button>
        <el-button type="warning" @click="confirmIgnore">确认忽略</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { WarningFilled, Lock, Unlock, Tips } from '@element-plus/icons-vue'
import dayjs from 'dayjs'
import api from '@/api'
import { useAppStore } from '@/store/app'
import type { Alarm, Person } from '@/api'
import {
  AlarmTypeMap, AlarmLevelMap, AlarmStatusMap
} from '@/utils/enumMaps'

const appStore = useAppStore()

const loading = ref(false)
const resolving = ref(false)

const statusFilter = ref<number | null>(null)
const typeFilter = ref<number | null>(null)
const levelFilter = ref<number | null>(null)

const resolveDialogVisible = ref(false)
const ignoreDialogVisible = ref(false)

const currentAlarm = ref<Alarm | null>(null)
const requiresRectification = ref(false)

const supervisors = computed<Person[]>(() => appStore.persons.filter(p => p.role === 3 && p.isActive))

const resolveForm = reactive({ action: '', remarks: '', handledById: undefined as number | undefined })
const ignoreForm = reactive({ handledById: undefined as number | undefined, reason: '' })

const filteredAlarms = computed(() => {
  let list = appStore.alarms
  if (statusFilter.value !== null) list = list.filter(a => a.status === statusFilter.value)
  if (typeFilter.value !== null) list = list.filter(a => a.alarmType === typeFilter.value)
  if (levelFilter.value !== null) list = list.filter(a => a.alarmLevel === levelFilter.value)
  return list.sort((a, b) => dayjs(b.alarmTime).valueOf() - dayjs(a.alarmTime).valueOf())
})

const pendingCount = computed(() => appStore.alarms.filter(a => a.status === 1).length)
const criticalCount = computed(() => appStore.alarms.filter(a => a.alarmLevel === 3 && a.status !== 3 && a.status !== 4).length)
const processingCount = computed(() => appStore.alarms.filter(a => a.status === 2).length)
const todayResolvedCount = computed(() => {
  const today = dayjs().format('YYYY-MM-DD')
  return appStore.alarms.filter(a =>
    a.status === 3 && a.resolvedTime && dayjs(a.resolvedTime).format('YYYY-MM-DD') === today
  ).length
})

const blockingAlarms = computed(() =>
  filteredAlarms.value.filter(a => a.blocksLiftingOperation && a.status !== 3 && a.status !== 4)
)

const formatDateTime = (d: string) => dayjs(d).format('YYYY-MM-DD HH:mm:ss')

const startProcess = async (alarm: Alarm) => {
  if (!supervisors.value.length) {
    await appStore.fetchPersons()
  }
  ElMessageBox.prompt('请选择处理人（监理）', '开始处理报警', {
    inputType: 'select',
    inputOptions: supervisors.value.map(s => ({ value: String(s.id), label: s.name }))
  }).then(async ({ value }) => {
    try {
      await api.alarms.startProcessing(alarm.id, Number(value))
      ElMessage.success('已开始处理')
      await appStore.fetchAlarms()
    } catch (e) { /* handled */ }
  }).catch(() => {})
}

const handleAlarm = (alarm: Alarm) => {
  if (alarm.status === 1 || alarm.status === 2) resolveAlarm(alarm)
}

const resolveAlarm = async (alarm: Alarm) => {
  if (!supervisors.value.length) await appStore.fetchPersons()
  currentAlarm.value = alarm
  requiresRectification.value = alarm.alarmLevel === 3 || alarm.alarmType === 1
  Object.assign(resolveForm, { action: '', remarks: '', handledById: undefined })
  resolveDialogVisible.value = true
}

const confirmResolve = async () => {
  if (!resolveForm.handledById || !resolveForm.action.trim()) {
    ElMessage.warning('请填写完整信息')
    return
  }
  resolving.value = true
  try {
    await api.alarms.resolve(
      currentAlarm.value!.id,
      resolveForm.action,
      resolveForm.remarks,
      requiresRectification.value
    )
    ElMessage.success({
      message: requiresRectification.value ? '已解决，整改单已自动生成' : '报警已解决',
      duration: 3000
    })
    resolveDialogVisible.value = false
    await Promise.all([
      appStore.fetchAlarms(),
      appStore.fetchRectifications(),
      appStore.fetchLiftingTasks(),
      appStore.fetchTowerCranes()
    ])
  } finally {
    resolving.value = false
  }
}

const ignoreAlarm = (alarm: Alarm) => {
  currentAlarm.value = alarm
  Object.assign(ignoreForm, { handledById: undefined, reason: '' })
  ignoreDialogVisible.value = true
}

const confirmIgnore = async () => {
  if (!ignoreForm.handledById || !ignoreForm.reason.trim()) {
    ElMessage.warning('请填写完整信息')
    return
  }
  try {
    await api.alarms.ignore(currentAlarm.value!.id, ignoreForm.handledById, ignoreForm.reason)
    ElMessage.success('已忽略报警')
    ignoreDialogVisible.value = false
    await Promise.all([appStore.fetchAlarms(), appStore.fetchLiftingTasks(), appStore.fetchTowerCranes()])
  } catch (e) { /* handled */ }
}

const viewDetail = (alarm: Alarm) => {
  ElMessageBox.alert(
    `
      <div style="line-height:1.8">
        <p><b>编号：</b>AL-${alarm.id}</p>
        <p><b>类型：</b>${AlarmTypeMap[alarm.alarmType].label}（${AlarmLevelMap[alarm.alarmLevel].label}）</p>
        <p><b>塔吊：</b>${alarm.towerCrane?.craneNo || '—'}</p>
        <p><b>关联任务：</b>${alarm.liftingTask?.taskNo || '—'}</p>
        <p><b>报警时间：</b>${formatDateTime(alarm.alarmTime)}</p>
        <p><b>描述：</b>${alarm.description}</p>
        ${alarm.handleAction ? `<p><b>处理动作：</b>${alarm.handleAction}</p>` : ''}
        ${alarm.handleRemarks ? `<p><b>备注：</b>${alarm.handleRemarks}</p>` : ''}
      </div>
    `,
    '报警详情',
    { dangerouslyUseHTMLString: true, confirmButtonText: '关闭', width: '500px' }
  )
}

const simulateAlarm = () => {
  ElMessageBox.prompt('请输入要模拟的塔吊ID', '模拟黑匣子报警', {
    inputType: 'input',
    inputValue: '1',
    inputValidator: v => !!Number(v) || '请输入数字ID'
  }).then(async ({ value }) => {
    const craneId = Number(value)
    const types = [
      { type: 1, level: 3, desc: '超载报警：实际载荷5.2吨超过额定载荷5.0吨', load: 5.2, pct: 104 },
      { type: 3, level: 3, desc: '幅度限位报警：工作半径62m超过最大60m', radius: 62 },
      { type: 6, level: 2, desc: '风速超限报警：瞬时风速18m/s', wind: 18 }
    ]
    const t = types[Math.floor(Math.random() * types.length)]
    try {
      await api.alarms.create({
        towerCraneId: craneId,
        alarmType: t.type,
        alarmLevel: t.level,
        description: t.desc,
        loadValue: (t as any).load,
        loadPercentage: (t as any).pct,
        radiusValue: (t as any).radius,
        windSpeed: (t as any).wind,
        requiresRectification: t.level === 3
      })
      ElMessage.success({
        message: '模拟报警已触发，请在列表中查看处理',
        type: t.level === 3 ? 'error' : 'warning',
        duration: 3000
      })
      await Promise.all([
        appStore.fetchAlarms(),
        appStore.fetchLiftingTasks(),
        appStore.fetchTowerCranes()
      ])
    } catch (e) { /* handled */ }
  }).catch(() => {})
}

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([appStore.fetchAlarms(), appStore.fetchPersons()])
  } finally {
    loading.value = false
  }
})
</script>
